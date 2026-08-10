# Register PDF rendering (PDFsharp / MigraDoc)

This document explains the non-obvious decisions behind `PdfSharpRegisterRenderer`,
`RegisterTableLayout`, `RegisterCellText` and `EmbeddedFontResolver`. Several of the choices
here look like they could be simplified or reverted by someone unfamiliar with MigraDoc's
behaviour. Each one below is a real defect that was found and fixed, not a hypothetical - read
this before changing page setup, margins, fonts, column widths, cell text or timestamps.

## 1. Page size must be set with explicit width and height, never `PageFormat.A4`

`Compose` sets:

```csharp
section.PageSetup.PageWidth = Unit.FromMillimeter(297);
section.PageSetup.PageHeight = Unit.FromMillimeter(210);
```

This looks redundant with MigraDoc's `PageFormat` enum, but it is not optional. Setting
`section.PageSetup.PageFormat = PageFormat.A4` alone leaves `PageWidth` reading `0.00mm`. That
zero then feeds into the content-width calculation (`PageWidth - LeftMargin - RightMargin`),
producing a **negative** content width. MigraDoc does not throw on a negative or negative-derived
column width - it accepts it silently. This has produced a plausible-looking 209-page PDF from a
single negative-width render, with no exception anywhere in the pipeline.

Also note: `section.PageSetup.Orientation` is a no-op once `PageWidth`/`PageHeight` are set
explicitly. Setting portrait dimensions (210mm x 297mm) and `Orientation.Landscape` still renders
portrait. Orientation is only meaningful when using `PageFormat`, which this renderer deliberately
does not use. If the register ever needs to support both orientations, swap `PageWidth` and
`PageHeight` explicitly - do not reach for `Orientation`.

## 2. Three PDFsharp 6.2.4 APIs are `[Obsolete]` and will not compile here

`EffectivePageWidth`, `PdfDocumentOpenMode.ReadOnly` and `PdfDocumentOpenMode.InformationOnly` are
all marked `[Obsolete]` in PDFsharp 6.2.4. This repository builds with `TreatWarningsAsErrors`, so
using any of them is a build error, not a warning that can be ignored.

- Use `section.PageSetup.PageWidth` (authoritative because `Compose` sets it explicitly per item
  1 above), not `EffectivePageWidth`.
- Use `PdfDocumentOpenMode.Import` when reopening a rendered PDF (for tests or inspection), not
  `ReadOnly` or `InformationOnly`. `Import` is the supported mode and exposes `PageCount` and page
  dimensions.

## 3. MigraDoc does not reserve body space for headers and footers

Unlike QuestPDF's `page.Header()`, which pushes the content area down automatically, MigraDoc's
`HeaderDistance` and `FooterDistance` only position the header/footer *frames* relative to the
page edge. They do nothing to the body's `TopMargin`/`BottomMargin`. Left alone, the table body
overlaps the header and footer on every page - in practice this overprinted the header across the
first five data rows and the footer across the last row of every page.

The fix is to enlarge `TopMargin` and `BottomMargin` by hand so the body clears the header/footer
content:

```
TopMargin    = HeaderDistance + HeaderReservedHeight
BottomMargin = FooterDistance + FooterReservedHeight
```

The reservation constants (`HeaderReservedHeightPoints = 91`, `FooterReservedHeightPoints = 19`)
are sized for the **worst case**: both optional header lines present (the "Registration No" line
and the company address line). They were derived by rendering that six-line header, reading the
actual glyph baselines back out of the generated PDF's content stream, and adding the embedded
Roboto font's ascent/descent plus the layout's 8pt header/table gap:

- Header: ascent(Bold 14) 14.67pt + line-to-line offsets 65.66pt + descent(Regular 8) 2.17pt +
  8pt gap = ~90.5pt, rounded up to 91pt.
- Footer: ascent(Regular 8) 8.38pt + descent(Regular 8) 2.17pt + 8pt gap = ~18.55pt, rounded up
  to 19pt.

If a document omits the registration number or address, the reservation is larger than strictly
needed for that document - that is intentional. A per-document reservation would need to be
recomputed whenever the header content changes, and an under-reservation silently reintroduces
the overprint defect. If you change what the header renders (font size, an added line, wrapped
text), you must re-derive these constants the same way, not guess at them.

## 4. `GlobalFontSettings.FontResolver` may be assigned exactly once per process

PDFsharp allows the font resolver to be set exactly once per process, and never after the first
`XFont` has been created - a second assignment throws. That is why `PdfSharpRegisterRenderer`
assigns it in a **static constructor**, which .NET guarantees runs at most once per process,
regardless of how many renderer instances are created or which thread creates the first one.

No test may assign `GlobalFontSettings.FontResolver` itself, even indirectly by constructing a
renderer in an unusual order. Doing so races the static constructor and can poison every other
test in the assembly that touches PDFsharp, because the resolver is process-wide, not
per-instance and not per-test-class. If a test needs to exercise `EmbeddedFontResolver` in
isolation, call its methods directly (`ResolveTypeface`, `GetFont`) rather than assigning it to
`GlobalFontSettings`.

## 5. Fonts are embedded because the production container has none, and bold must be a real face

The production image is chiseled with no system fonts, so an unresolved family lookup would be a
runtime failure mid-export rather than a build-time or startup problem. `EmbeddedFontResolver`
reads `Roboto-Regular.ttf` and `Roboto-Bold.ttf` from assembly-embedded resources and is total by
design: any requested family falls back to the embedded Roboto rather than returning null.

PDFsharp implements **italic simulation** (it can slant a regular face algorithmically) but does
**not** implement bold simulation. If only the Regular face were embedded, bold requests would
silently render as unbolded regular text - headings, the header company name, and the table
header row would all lose their bold weight with no error anywhere. This is why both a Regular
and a real Bold TTF are embedded, and why `ResolveTypeface` maps `bold: true` to the Bold face
name rather than asking PDFsharp to simulate it.

## 6. Rendering is serialised behind a process-wide lock

PDFsharp's global font cache and font-resolution machinery are not thread-safe. `Render` is called
concurrently: the renderer is registered as a **singleton** and invoked from parallel request
threads.

The race was reproduced independently in two separate experiments, run at different times against
different builds, with different document shapes and different detection methods, both under
32-way parallelism (`Parallel.For` with `MaxDegreeOfParallelism` well above 1) with the lock
removed:

- Experiment A, against the original unlocked renderer, 30-row documents, comparing rendered
  content streams: 12 of 384 documents deviated, about 3 percent.
- Experiment B, a deliberate positive control run after the lock was added, using a byte-identical
  replica with the lock removed, 50-row documents, comparing both byte length and content-stream
  hashes: 250 of 384 deviated by length and 258 of 384 by content-stream hash, about 65 percent.

Both experiments recorded **zero** deviations across 384 documents with the lock in place.

Do not read either number as *the* corruption rate. The observed rate varies with document shape,
degree of parallelism and how corruption is detected - it ranged from roughly 3 percent to roughly
65 percent between these two runs alone, and a different shape or a different concurrency level
could land outside that range in either direction. The only reliable statement is that the race
exists, it is not rare enough to ignore, and the lock removes it entirely in every experiment run
so far.

Critically, the failure mode is **silent**: rendering never throws. Corruption shows up as a
spurious font switch injected mid-cell into the PDF content stream - the resulting PDF still opens
and looks plausible without careful inspection. A passing functional test suite is not evidence
that concurrent rendering is safe, because ordinary assertions (`"%PDF"` prefix, non-empty bytes,
row/column counts) do not look inside the content stream closely enough to catch it.

Serialising every export process-wide is an accepted cost: the largest register (2000 rows, see
section 12) renders in a few seconds, and register PDF exports are infrequent admin operations, not
a hot request path. Do not remove or narrow this lock without re-running a high-concurrency
corruption test of the kind described above, comparing byte length and content-stream hashes across
many parallel renders of the same document - the failure mode does not throw, so ordinary test
assertions will not catch a regression here.

## 7. Column widths are absolute; MigraDoc never scales a table to fit

MigraDoc column widths are absolute point values, not proportions. Unlike some layout engines,
MigraDoc does **not** shrink a table to fit the page if the widths overshoot the content box -
columns that don't fit are simply pushed off the page, silently.

`RegisterTableLayout.ColumnWidths` guarantees the returned widths always sum to exactly the
content width by:

1. Reserving a minimum-width floor (currently 1pt per column) for every column first, so no
   column can collapse to zero or a negative width even with a zero or missing weight.
2. Splitting only the width remaining after the floor is reserved, proportionally by weight.
3. Giving the **last column** whatever remainder is left over after the others are assigned, so
   floating-point rounding drift can never push the total past the content width.

If the content width is too small to honour the floor for every column at all, the floor is
dropped and the width is split evenly instead - an exact-sum table with a hairline column is a
better failure than a table that silently overflows the page.

## 8. Cell text is sanitised because MigraDoc turns tabs/newlines into layout, not text

`RegisterCellText.Sanitise` collapses any run of whitespace or control characters (including tabs
and newlines) inside a cell value down to a single space, and trims the ends. This is necessary
because MigraDoc does not treat `\t` or `\n` inside paragraph text as literal characters - it
parses them into real layout nodes (tab stops, line breaks). In the register table, which is
already row-height-sensitive because of item 3 above, an unsanitised value with an embedded
newline silently adds an extra line to that cell's row height, which shifts pagination for every
row after it. There is no exception; the document just paginates differently than expected.

## 9. PDF metadata timestamps are stamped as true UTC instants, not local wall-clock

`RegisterDocument.GeneratedAt` is South African wall-clock time with `DateTimeKind.Unspecified`
(South Africa has no DST, a constant UTC+2). PDFsharp derives the PDF's `/CreationDate` offset
from the `DateTime.Kind` of the value it is given, not from the host machine's actual time zone
setting. An `Unspecified` Kind gets stamped with the **host machine's own local UTC offset** -
which happens to be correct by accident on a developer's UTC+2 machine, and is two hours wrong
under the production container, which runs on UTC.

The renderer converts `GeneratedAt` to a true UTC instant with `DateTimeKind.Utc` via
`TimeZoneInfo.ConvertTimeToUtc(document.GeneratedAt, SouthAfricaTimeZone.Instance)` before handing
it to `PdfDocument.Info.CreationDate`/`ModificationDate`. With a genuine `Kind.Utc` value, PDFsharp
writes a real `+00'00` offset regardless of the host's local time zone. Do not pass `GeneratedAt`
through unconverted, and do not "fix" this by reading the host's local time zone instead - the
host's time zone is irrelevant to what the document should say, only the conversion from SAST
matters.

## 10. Page count is content-dependent - there is no single number, only a figure per dataset

An earlier version of this document claimed the register produces "about 17% more pages than the
old QuestPDF renderer", based on a 5000-row export producing "about 209 pages" against "roughly
179" for QuestPDF. Both halves of that claim are stale and have been removed. First, the PDF
export cap is now 2000 rows (item 12), so a 5000-row PDF export cannot happen at all - the
scenario the old figures described no longer exists. Second, the 209-page figure predates the
break-opportunity work in item 11 entirely: it was measured before `RegisterCellText` wrapped any
cell content, so it does not reflect what the shipped renderer actually produces at
`LongRunThreshold = 5` and `BreakInterval = 3`.

Page count depends entirely on which dataset produced it, so there is no single figure - two
datasets are given below, both re-measured at the shipped settings (`LongRunThreshold = 5`,
`BreakInterval = 3`, Release configuration, standalone harness), and it matters which one you are
looking at.

**The realistic case.** A dataset that mirrors an actual South African safe custody register:
dates for stored-from/stored-to, makes such as `CZ` and `Glock`, models, calibres, `Handgun` and
`Rifle` in the `Type` column, serial numbers such as `SN0000-XY00`, full owner names, 11-digit ID
numbers, **one** wrapping street address, licence numbers such as `WC/2020/00000`, a reason,
released-to, an occasional two-word remark, and a blank signature column - long values in roughly
four of the sixteen columns, not all of them:

| Rows | Pages | Rows/page | Output size |
|------|-------|-----------|--------------|
| 60   | 6     | 10.0      | 82 KB |
| 200  | 20    | 10.0      | 180 KB |
| 2000 | 200   | 10.0      | 1442 KB |

Rows/page is the figure that actually transfers between dataset sizes - it stays flat at 10.0 here
across a 33x range in row count, because it is set by the tallest wrapping cell in each row (the
street address, which already wrapped to three lines under ordinary word-wrap alone, before any
`U+200B` break opportunity existed) rather than by row count.

**A genuinely reassuring finding, easy to miss and worth stating plainly: on this realistic
dataset, adding break opportunities did not increase the page count at all.** Rows/page measured
10.0 both before this fix and after it. The reason is that the inserted breaks fit inside the row
height the wrapping address column was already forcing - a typical address's individual words are
short enough that ordinary word-wrap (which MigraDoc supported before this work) already broke the
line in the same places, so the character-level break opportunities this fix adds had nothing
extra to do for that column. A reader who has only seen the worst-case figure below could otherwise
conclude this fix made every printed register three times longer - that is false for realistic
data, and this line exists so nobody draws that conclusion from the wrong fixture.

**The worst case.** `PdfSharpRegisterRendererPerformanceTests.RealisticRow`, the fixture this
document has otherwise called "realistic" up to this point, is not representative of production
data - it deliberately stacks a long value into essentially every one of the 16 columns (a long
owner name, a wrapping address, a purpose sentence, a free-text remark paragraph, and more), which
no real safe custody row looks like. It exists on purpose, as a pessimistic guard: the performance
test in item 12 uses it deliberately so that the render-time budget is tested against a genuinely
worst-case row shape, not an average one, and this document has used the same shape elsewhere
(items 11 and 12) so that its render-time and byte-count figures describe a guaranteed upper bound
rather than a typical case. At 2000 rows it renders to 667 pages, 2,729,605 bytes - 3.0 rows/page,
about a third of the realistic case's density. An earlier version of this document mislabelled a
second attempt at a lighter fixture as "moderate" and reported it also landing at 667 pages,
concluding that trimming column content didn't matter; that fixture was itself still pessimistic
(too many columns still carried long, frequently-wrapping values, including an address heavier
than a typical one) and the "moderate versus pessimistic tie" conclusion drawn from it has been
removed as a result - it was an artefact of comparing two worst-case fixtures against each other,
not a real finding about typical data.

For contrast, a fixture with genuinely trivial values in every column (4-character strings like
`r0c0`, none of them crossing `LongRunThreshold`, the shape the performance test's fixture used
before it was rewritten) renders the same 2000 rows to only 141 pages, about 14.2 rows/page -
confirming again that the driver is which specific fields wrap and by how much, not row count.

This document no longer states a QuestPDF page-count comparison. QuestPDF was removed from the
codebase during this migration, so it cannot be re-rendered to check, and the old "roughly 179
pages" figure was measured at 5000 rows, a size this renderer's PDF export can no longer even
request (item 12) - it is not comparable to any figure above at any row count that still applies.
Item 12 keeps a QuestPDF *render time* comparison (about 1 second for 5000 rows) because that is an
independent historical fact about QuestPDF's own speed, not tied to the row cap that has since
changed on this side; no equivalent page-count fact is retained here because none can be verified
at current settings.

If you need to reason about a future dataset's page count, measure it directly against the shipped
`LongRunThreshold`/`BreakInterval`, with a fixture that actually matches how many columns carry
long values in production, rather than assume either figure above - the realistic and worst-case
fixtures differing by more than 3x on rows/page shows that how many columns are long matters far
more than how long any single value is.

## 11. Long unbroken cell values overflow their column unless given a U+200B break opportunity

QuestPDF broke long tokens at the character level and kept them inside their cell. MigraDoc has
no equivalent "break anywhere" setting - a long unbroken run of characters (a licence number, an
ID number, a single long word) is laid out as one unbreakable unit and simply overprints whatever
column comes next once it runs past the cell's right edge. This is not cosmetic: on a 16-column
safe custody register at 8pt, the licence number `WC/2020/00000` ran into the next column
(`Owner Name`, which immediately follows `Licence Number` in this register's real column order),
`Handgun` in the `Type` column bled into its neighbour `Calibre`, and address text bled from
`Address` into `Purpose`. There is no exception and no warning - the content stream is valid, the
PDF opens fine, and the defect is only visible on inspection.

MigraDoc does, however, honour `U+200B` (ZERO WIDTH SPACE) as a line-break opportunity, the same
way it already treats an ordinary space or a hyphen. `RegisterCellText.InsertBreakOpportunities`
walks each maximal run of non-whitespace characters in a cell value and, if the run is longer than
`LongRunThreshold`, inserts a `U+200B` every `BreakInterval` characters in that run (not after
every character - see "Why the interval is 3, not every character" below). Runs at or below the
threshold are returned untouched. Break opportunities are spaced through the run rather than tied
to natural points such as slashes, because the real defects included values with no natural break
point at all (a bare 13-digit ID number, `Handgun` as a single word).

### Why the threshold is 5

`LongRunThreshold` is derived from the narrowest real column in the safe custody register, not
guessed - but an earlier version of this derivation was itself arithmetically wrong, computed the
usable width about 25% too generously, and shipped a threshold of 6 that still let some
exactly-6-character runs (`000000`, `888888`, a run like `MMMMMM`) overflow. The corrected
derivation accounts for two things the first version missed:

1. **The 1pt-per-column floor.** `RegisterTableLayout.ColumnWidths` (item 7 above) reserves 1pt
   for every column before distributing the remaining width by weight - it is not a pure
   weight-proportional split of the full content width. With 16 columns, weights summing to 16.6,
   the narrowest column at weight 0.8, and `ContentWidth` 793.8898pt (A4 landscape, 24pt margins
   each side):

   ```
   floor budget   = 16 * 1pt = 16pt
   distributable  = 793.8898 - 16 = 777.8898pt
   column width   = 1 + (777.8898 * 0.8 / 16.6) = 38.4887pt
   ```

   Not `793.89 * (0.8 / 16.6) = 38.26pt` as an earlier version of this document computed - that
   formula silently drops the floor reservation and understates every column's real width contest,
   including the narrowest one.

2. **MigraDoc's own default cell padding, plus the cell border.** `AddCellText`'s caller sets a 3pt
   `LeftIndent`/`RightIndent` on each `Column`, but MigraDoc applies its *own* default cell padding
   of 0.12cm on top of that, and this renderer never overrides it. 0.12cm is 3.4016pt. On top of
   *that*, the table's 0.5pt `BorderWidth` also consumes usable space - a second, independent
   review's empirical wrap bisection in the `Type` column (rendering successive strings and finding
   the exact character where wrapping starts, rather than trusting the padding arithmetic alone)
   bracketed the true usable width between 24.9062pt (fits on one line) and 25.2148pt (wraps) -
   narrower than padding alone predicts. The 0.5pt border is what accounts for the gap: a
   padding-only prediction of the text origin (column left 197.199 + 3pt indent + 3.4016pt MigraDoc
   padding = 203.1006) undershoots the actually-rendered `Td` x-position of 203.6005 by almost
   exactly 0.5pt, matching `BorderWidth`. So the real usable text width subtracts padding on both
   sides *and* the border:

   ```
   usable text width = 38.4887 - 2 * (3 + 3.4016) - 0.5 = 38.4887 - 12.8031 - 0.5 = 25.1856pt
   ```

   25.1856pt sits inside the empirically-bisected [24.9062, 25.2148] bracket, which is the stronger
   confirmation - the bisection measures the real wrap point directly by rendering, rather than by
   reasoning about padding constants that could themselves be incomplete.

Do not try to reclaim this width by reducing `CellPadding` or overriding MigraDoc's default cell
padding - either would change the visual density of the whole register's columns, which is a
separate, out-of-scope decision from fixing this threshold.

At 25.1856pt, the question is again how many 8pt embedded-Roboto characters fit, measured (not
estimated) with PDFsharp's `XGraphics.MeasureString` against the same embedded `Roboto-Regular.ttf`:

- Digits (`0123456789`): 4.4922pt/char average, so 25.1856 / 4.4922 is about 5.61 characters. This
  does not change the threshold versus the padding-only estimate of 5.72 - both round down to 5.
- The exactly-6-character overflow cases a geometric review found still crossing the text box under
  the old threshold of 6: `000000` and `888888` each measure 26.953pt, `Damagd` measures 29.961pt,
  and `MMMMMM` (deliberately the widest realistic letter, repeated) measures 41.906pt - all past
  25.1856pt. That review rendered a register containing 6-character runs and recorded 36 runs past
  the text box and 12 crossing into the neighbouring column, `MMMMMM` visibly overprinting
  `Calibre` from `Type`.

`LongRunThreshold = 5`: runs of 6 or more characters get break opportunities; runs of 5 or fewer
stay untouched. Unlike the threshold-6 derivation's off-by-one reasoning about `Handgun`, there is
no ambiguity here - 5.61 rounds down to 5 cleanly, and 5 is the largest threshold that does not
leave any of the confirmed 6-character overflow cases untouched. Short values - `Glock`, `SN123`,
`Muizenberg` split as two words by earlier column data, ordinary model names - stay untouched
because their runs are 5 characters or fewer.

**What threshold 5 does not guarantee.** Runs of 4 or 5 characters receive no break opportunity at
all, and a wide enough 5-character run can still exceed the 25.1856pt text box and cross the column
border - this is a content-dependent guarantee, not a geometric one. Measured against a
31.5871pt border-crossing limit (`column width 38.4887 - left-side padding-and-border 6.9016`,
the point at which text would actually reach the neighbouring column's territory, a larger figure
than the 25.1856pt text-box limit because the cell's own right-side padding and the neighbour's
left-side padding both provide slack before a real collision): `MMMMM` measures 34.92pt, `WWWWW`
measures 35.49pt and `@@@@@` measures 35.92pt, all past 31.5871pt - so an unusually wide
5-character run of repeated capital letters can genuinely cross into `Calibre`. Realistic
5-character values measured well inside the border: `SMITH` 24.38pt, `WORLD` 27.07pt, `AMMOS`
29.43pt.

Threshold 4 would remove this possibility entirely - `@@@@`, the widest realistic 4-character run
measured, is 28.73pt, under the 31.5871pt crossing limit even for degenerate content. It was
deliberately rejected: threshold 4 would also break every ordinary 5-character value, and `Rifle`
is one of the most common values the narrow `Type` column actually holds in this domain - it fits
comfortably today, untouched, at threshold 5. Breaking `Rifle` into `Rifl` and `e` to defend
against a pathological input like `MMMMM` is a bad trade for a register that is read and signed by
people, not just measured by tooling. A future maintainer who wants to close this last gap should
understand that the tradeoff was seen and rejected on readability grounds, not missed.

### Why the interval is 3, not every character

The first version of this fix inserted `U+200B` between every character of a run above the
threshold. That is much stronger than the defect requires. MigraDoc breaks a line at the *last*
break opportunity that still fits, not at every opportunity, so a break opportunity every
`BreakInterval` characters is sufficient to prevent overflow: a cell that fits `N` characters
simply breaks at the nearest opportunity at or before the edge, with no overhang, whether the
opportunities are 1 or 3 characters apart. What every-character insertion bought beyond that was
tighter packing at a real cost: three times as many inserted characters means three times as many
distinct text-showing operations for MigraDoc's layout engine to measure and position, and that
turned out to dominate render time at scale (see "Measured consequences" below).

`BreakInterval = 3` was chosen by measurement, the same way `LongRunThreshold` was: rendering the
realistic 5000-row dataset at intervals of 1, 3 and 4 showed 3 gives a large, real improvement over
every-character insertion while staying comfortably under the performance budget. Interval 4 was
tried and rejected - it did not continue the improvement. Coarser breaks give MigraDoc's line
formatter fewer places to end a line inside a narrow column, and in this data that pushed some
lines mid-chunk past where a finer break would have fit, producing *more* wrapped lines, not fewer,
which made both the page count and the render time worse than interval 3, not better. Do not raise
`BreakInterval` above 3 without re-measuring on realistic data the same way - "coarser" does not
reliably mean "cheaper" once a chunk stops fitting as cleanly against a narrow column's edge.

**Interval 3 stays safe at the corrected 25.1856pt usable width, but the margin is not large and
not structural.** The widest realistic 3-character chunks measured in embedded Roboto 8pt are
`@@@` at 21.551pt, `WWW` at 21.293pt and `MMM` at 20.953pt - all comfortably under 25.1856pt, with
roughly 3.4 to 3.7pt to spare (measured against the empirically-bisected 24.9062pt conservative
bound, the safer figure to margin against since it is a directly observed wrap point rather than a
derived one). That margin exists only because `LongRunThreshold` and `BreakInterval` were both
derived against *this* register's actual narrowest column (weight 0.8 of 16.6 total, 16 columns).
`RegisterTableLayout` guarantees only a 1pt-per-column floor (item 7) - it makes no promise about
how narrow the narrowest weighted column can get. A future register with more columns, a smaller
minimum weight, or a near-zero weight column could produce a narrower real column than 38.4887pt,
shrink the 25.1856pt usable width further, and reintroduce overflow at interval 3 even with
`LongRunThreshold` unchanged. Re-run this same measurement (rendered geometry against
`XGraphics.MeasureString`, not estimation) before assuming interval 3 is still safe for a
materially different column layout.

### Confined to table cells only

`InsertBreakOpportunities` is only called from `PdfSharpRegisterRenderer.AddCellText`, which is
used for both the column header row and every data row. It is deliberately not called from the
company/title header block, the period line, the generated-by line, the footer, or the
empty-state paragraph. Those are full-width single-line strings with the entire content width
available to them - they cannot overflow the way a narrow table column can - so inserting
invisible characters into them would only pollute the document's most-read text (the company name,
the report title, the "Generated by" line) for no layout benefit. `RegisterCellText.Sanitise` and
`InsertBreakOpportunities` compose cleanly: a cell value is sanitised first (whitespace and control
characters collapsed to single spaces) and then given break opportunities, and `U+200B` is neither
whitespace (`char.IsWhiteSpace`) nor a control character (`char.IsControl`), so `Sanitise` passes it
through unchanged and the two functions never fight over the same characters.

### Accepted cost: the text layer is chunked, not annotated with zero-width spaces

An earlier version of this document claimed that a genuine `U+200B` sits between glyphs in the
rendered PDF's text layer, and that copying text out carries those characters along. **That claim
is not true, and has been corrected.** A geometric review scanned the per-page decompressed content
streams and the whole file via `qpdf --qdf`, checking for `U+200B` encoded as UTF-8 (`e2 80 8b`),
UTF-16BE (`20 0b`) and the literal ASCII string `200B`, and found zero occurrences anywhere in a
rendered PDF. MigraDoc consumes `U+200B` purely as a line-breaking instruction during layout - it
decides where to end each line and never writes the character itself into the output. Confirmed
also in this document's own round 2 fix verification: a geometric check that parsed a rendered
page's content stream found the `Type` column's `000000` example emitted as two separate `Tj`
operators, `(000) Tj` and `(000) Tj`, not one `(000` + `U+200B` + `000) Tj` run - there is no
zero-width space character anywhere in the string PDFsharp writes.

The actual accepted cost is different: **each chunk becomes its own `Tj` text-showing operator**,
and the document carries no `/ToUnicode` CMap. For a value like `Handgun` (broken into `Han`,
`dgu` and `n` at `BreakInterval = 3`), the content stream contains `(Han) Tj` followed by
`(dgu) Tj` and `(n) Tj` as three independent operators positioned next to each other, not one
continuous string.
A text extractor or a copy-paste operation that reconstructs page text by concatenating `Tj`
operator contents, rather than reasoning about their adjacent positions, may reproduce this as
`Han dgu n` or similar - inserting visible whitespace at chunk boundaries that was never in the
source data, rather than an invisible character. This is still an accepted cost, not an oversight:
the alternative is the overflow defect itself, a correctness problem on a register that is
printed, signed and inspected. If a future consumer needs extracted or copied text to be
byte-identical to the typed value, that requires either measuring column width per-document (this
renderer does not do that - column widths are only known after layout, whereas cell text is
composed before layout), embedding a `/ToUnicode` CMap that maps chunk boundaries back to the
original unbroken string, or switching to a text layout approach with real subword
break-anywhere support - not a change to this threshold.

**This has not been verified against a real PDF viewer's copy-paste behaviour.** Neither
`pdftotext` nor `mutool` was available in the environment used to derive the above - the claim
about `Han dgun`-style reconstruction is inferred from the content stream structure (separate `Tj`
operators, no `/ToUnicode` CMap), not observed by actually selecting text in a viewer and reading
the clipboard. Before relying on this section for anything user-facing, someone should open a
rendered register in a real PDF viewer, select and copy a licence number and an ID number that
crossed the threshold, and confirm what actually reaches the clipboard.

### Measured consequences

Rendering the same 16-column safe custody register data (30 rows, the pessimistic fixture shape -
licence numbers, 13-digit ID numbers, a long address, a long remark in most of the 16 columns; the
same shape as `PdfSharpRegisterRendererPerformanceTests.RealisticRow`) with and without this fix,
re-measured against the shipped `LongRunThreshold = 5`, `BreakInterval = 3` (Release, standalone
harness):

- Page count: 8 pages with no fix, 10 pages at the shipped settings. Wrapping instead of
  overflowing makes cells taller when their long values wrap onto multiple lines, so rows grow and
  the document gets longer - this is the same tradeoff already accepted in item 10 above, now
  compounded by this fix.
- Output size for this 30-row document, no fix: 71,527 bytes. This does not depend on
  `LongRunThreshold` at all - no fix means no `InsertBreakOpportunities` call, so no threshold
  applies - and remains accurate regardless of which threshold has ever shipped.
- Output size for this 30-row document **at the shipped settings**
  (`LongRunThreshold = 5`, `BreakInterval = 3`, Release configuration, this document's own
  standalone harness): **81,023 bytes**, re-measured directly against the shipped build. An
  earlier version of this document recorded 80,654 bytes for "every 3 characters" - that number
  was measured at the old, superseded `LongRunThreshold = 6` and was stale; 81,023 bytes is the
  correct figure for what actually ships today. A separate review's own harness measured 80,755
  bytes for the same corrected scenario - a 0.33% difference from a different harness, not a
  regression; if a future re-measurement lands anywhere close to 81,023 bytes with this harness
  and these settings, treat it as confirmation, not drift.
- A third figure sometimes cited alongside these two, 83,767 bytes for "every character"
  (`BreakInterval = 1`), was also measured at the old threshold 6 and describes an interval that
  was rejected in an earlier round (see "Why the interval is 3, not every character" above). It is
  historical illustration of why every-character insertion was abandoned, not a claim about
  current output, and has not been re-measured at the shipped threshold because the shipped code
  no longer has an every-character code path to measure without temporarily reintroducing it.

**The originally committed performance test's fixture understated the true cost of this renderer
by roughly a factor of two and a half, independent of this fix.** Its rows use values like `r0c0`
(4 characters, every column, never crossing `LongRunThreshold`), which is not what a real safe
custody register looks like - real rows carry a full owner name, a wrapping street address and a
free-text remark in several columns. Measured *before any `U+200B` fix existed*, the trivial
fixture rendered 5000 rows in about 3.6s; the same 5000 rows with realistic column content (the
same shape as the 30-row comparison above, scaled up) took about 8.9s - about 2.5x longer, purely
from MigraDoc laying out more and longer text, before this fix ever inserted a single `U+200B`.
The performance test's fixture has been rewritten to use this realistic shape (see
`PdfSharpRegisterRendererPerformanceTests.RealisticRow`) so it exercises the actual cost profile of
a real export, including the code path this fix adds.

**Historical calibration evidence - `BreakInterval` was chosen against a 5000-row export and a
20s budget that no longer exist.** The PDF export row cap has since been lowered to 2000 rows (see
item 12) and `BudgetSeconds` has since been retuned twice, most recently to 10 (see item 12) - the
table below records the measurement that justified picking interval 3 over 1 or 4, at the row count
and budget in force when that measurement was taken. It is legitimate calibration data and is kept
for that reason, but none of the absolute numbers describe the renderer's current row cap or
budget; see item 12 for those.

Render time for a 5000-row export at each `BreakInterval` tried, at `LongRunThreshold = 6` (the
threshold in force at the time; it has since been corrected to 5, see "Why the threshold is 5"
above - the threshold correction changes which runs get broken, not the relative cost ordering
between intervals, so this comparison remains valid for choosing `BreakInterval`):

| Insertion            | Render time (realistic 5000 rows) | Output size  | Build configuration |
|-----------------------|-----------------------------------|--------------|----------------------|
| No fix (baseline)     | ~8.9s                              | 5,000,798 B  | Release (standalone harness) |
| Every character        | 20.1s - 22.8s | 7,223,919 B | Release (standalone harness) |
| Every 3 characters (chosen) | ~11.0s - 11.8s | 6,705,121 B | Release (standalone harness) |
| Every 3 characters (chosen) | ~11.1s - 19.9s | 6,705,121 B | Debug (`dotnet test`, no build flag) |
| Every 4 characters (rejected) | ~15.0s - 15.7s | 8,369,328 B | Release (standalone harness) |

Every-3 was a large, real improvement over every-character at the 5000-row size: roughly half the
render time and about 1.5MB less output in a Release build. Every-4 was tried and rejected: it was
slower than every-3 on every measurement taken (5000-row render time, output size, and page count
on a 30-row check), not faster. Coarser break spacing gave MigraDoc's line formatter fewer usable
break points inside the narrowest columns, which produced more wrapped lines overall rather than
fewer - see "Why the interval is 3, not every character" above. Neither of those relative
conclusions depends on the row cap or budget that were in force at measurement time, which is why
this table is retained even though the 5000-row size and any budget comparison against it are
superseded.

## 12. The PDF export cap is 2000 rows, not 20000 - CSV carries the larger extracts

`ExportStorageRegisterQueryHandler.MaxPdfExportRows` is 2000. `MaxExportRows`, the CSV cap, is
20000 and is unaffected by anything in this document - CSV export does not go through MigraDoc and
has no comparable per-row rendering cost.

The two caps differ by 10x because rendering happens synchronously inside the HTTP request, on the
render lock described in item 6, and MigraDoc is substantially slower than the QuestPDF renderer it
replaced. Render time was measured against realistic 16-column safe custody register data (the same
fixture shape as `PdfSharpRegisterRendererPerformanceTests.RealisticRow`), at `LongRunThreshold = 5`
and `BreakInterval = 3`, across the full row range. Every timing below states its build
configuration explicitly - Release and Debug differ by roughly 2-4x on this renderer, so a number
without a stated configuration is not comparable to anything else in this document:

| Rows | Render time | Build configuration |
|------|-------------|----------------------|
| 50   | ~60 ms      | Release (standalone harness) |
| 200  | ~250-300 ms | Release (standalone harness) |
| 500  | ~665-685 ms | Release (standalone harness) |
| 1000 | ~850 ms-1.1 s | Release (standalone harness) |
| 2000 | ~1.7-1.8 s  | Release (standalone harness) |
| 2000 | ~2.9-3.3 s  | Debug (`dotnet test`, no build flag) |
| 5000 | ~9.9-12.1 s | Release (standalone harness) |

The curve is roughly linear up to about 2000 rows and then worsens somewhat at 5000 as more cells
cross `LongRunThreshold` and pick up `U+200B` break opportunities (item 11) - the 5000-row figure is
not a pure straight-line extrapolation of the smaller sizes. QuestPDF rendered a 5000-row load in
about 1 second; MigraDoc does not stay inside a comfortable request budget at that size on a Debug
build, and even in Release it is an order of magnitude slower than QuestPDF was.

Capping the PDF export at 2000 rows keeps the worst case in the low single-digit seconds even in
Debug configuration, comfortably inside a request, instead of the multi-second-to-tens-of-seconds
range 5000 rows produced depending on build configuration and content. A user who needs more than
2000 rows exports CSV instead - CSV has no MigraDoc cost and stays at the original 20000-row cap.
The validation error `ExportStorageRegisterQueryHandler` returns when a PDF request exceeds the cap
already tells the user to narrow the date range or export CSV for wider ranges.

`BudgetSeconds` is 10, re-derived twice on this branch as the row cap and the threshold changed.
Three consecutive Debug-build runs against the current 2000-row cap and the corrected
`LongRunThreshold = 5` (2026-08-10, `dotnet test`, which builds Debug by default and has been
observed slower and more variable than Release throughout this document): 2896 ms, 2945 ms,
2868 ms. At a prior 17s budget, set against an earlier three-run measurement of 2902-3315ms, the
guard had roughly 5x headroom over the slowest observed run - comfortable, but wide enough that a
2x, 3x or even 4x regression (the same order of magnitude this MigraDoc migration itself produced
over QuestPDF) would not have tripped it. `BudgetSeconds = 10` keeps about 3x headroom over the
slowest Debug run observed here (2945ms x 3 is under 9s; 10 rounds up from that with a small
margin) while restoring real sensitivity to a 2x-4x regression. If a future measurement on this
fixture regularly lands outside roughly the 2.8-3.5s range in Debug configuration, re-derive the
budget the same way rather than assume this margin still holds.
