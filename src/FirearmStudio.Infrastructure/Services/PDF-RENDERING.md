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

## 10. The register now produces about 17% more pages than the old QuestPDF renderer - deliberately

A 5000-row export produces about 209 pages here against roughly 179 under the previous QuestPDF
renderer. This was measured, reviewed and accepted as a deliberate tradeoff, not an oversight to
be "optimised away" on sight:

- About 21 of the extra pages come from row height: this renderer's rows are 17.05pt tall against
  QuestPDF's 15.6pt.
- About 9 of the extra pages come from the worst-case header/footer reservation described in
  item 3.

If you need to close this gap, understand that doing so means retuning the same vertical metrics
that fix defect 3 (row height, header reservation, or both) - do not reduce
`HeaderReservedHeightPoints`/`FooterReservedHeightPoints` or the row spacing without re-deriving
them by the same measurement process described in item 3, or the header/footer overprint defect
will return.

## 11. Long unbroken cell values overflow their column unless given a U+200B break opportunity

QuestPDF broke long tokens at the character level and kept them inside their cell. MigraDoc has
no equivalent "break anywhere" setting - a long unbroken run of characters (a licence number, an
ID number, a single long word) is laid out as one unbreakable unit and simply overprints whatever
column comes next once it runs past the cell's right edge. This is not cosmetic: on a 16-column
safe custody register at 8pt, the licence number `WC/2020/00000` ran into the next column's date,
`Handgun` bled into Serial Number, and `Muizenberg` bled into Signature. There is no exception and
no warning - the content stream is valid, the PDF opens fine, and the defect is only visible on
inspection.

MigraDoc does, however, honour `U+200B` (ZERO WIDTH SPACE) as a line-break opportunity, the same
way it already treats an ordinary space or a hyphen. `RegisterCellText.InsertBreakOpportunities`
walks each maximal run of non-whitespace characters in a cell value and, if the run is longer than
`LongRunThreshold`, inserts a `U+200B` every `BreakInterval` characters in that run (not after
every character - see "Why the interval is 3, not every character" below). Runs at or below the
threshold are returned untouched. Break opportunities are spaced through the run rather than tied
to natural points such as slashes, because the real defects included values with no natural break
point at all (a bare 13-digit ID number, `Handgun` as a single word).

### Why the threshold is 6

`LongRunThreshold` is derived from the narrowest real column in the safe custody register, not
guessed. That register's column weights sum to 16.6, and its narrowest column has weight 0.8, over
a `ContentWidth` of 793.89pt (A4 landscape, 24pt margins each side):

```
column width = 793.89 * (0.8 / 16.6) = 38.26pt
available for text = 38.26 - (2 * CellPadding) = 38.26 - 6 = 32.26pt
```

At that point, the question is how many 8pt embedded-Roboto characters fit in 32.26pt. This was
measured, not estimated from a rule of thumb, by loading the same embedded `Roboto-Regular.ttf`
through PDFsharp's `XGraphics.MeasureString` at 8pt and measuring representative content:

- Digits (`0123456789`): 4.492pt/char average, so 32.26 / 4.492 is about 7.18 characters.
- A realistic mix of licence numbers, ID numbers, place names and common words
  (`WC/2020/00000 8501015800086 Muizenberg Handgun`): 4.301pt/char average, about 7.50 characters.
- The literal cited overflow example, `Handgun` (7 characters), measures 32.293pt on its own -
  already past the 32.26pt available width before any other column padding or rounding is
  considered.

A naive reading of "just below" the ~7.2-7.5 characters that fit would suggest a threshold of 7.
That would be wrong: it would leave `Handgun`, a real cited defect and exactly 7 characters,
untouched, because 7 is not greater than 7. Picking the threshold one step below what a
whole-word average suggests, 6, is what's needed to actually cover the cited real-world overflow:
any run of 7 or more characters gets break opportunities, and `Handgun` (7 characters, all
letters, no natural break point) is exactly the shape of value the fix exists for. Short values -
`Glock`, `SN1234`, `Muizenberg` split as two words by earlier column data, ordinary model names -
stay untouched because their runs are 6 characters or fewer.

### Why the interval is 3, not every character

The first version of this fix inserted `U+200B` between every character of a run above the
threshold. That is much stronger than the defect requires. MigraDoc breaks a line at the *last*
break opportunity that still fits, not at every opportunity, so a break opportunity every
`BreakInterval` characters is sufficient to prevent overflow: a cell that fits 7 characters simply
breaks after the 6th (the nearest opportunity at or before the edge), with no overhang, whether the
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

### Accepted cost: invisible characters in the text layer

Once a cell value crosses the threshold, every character in that run is separated by a real
`U+200B` in the PDF's text layer, not just visually - it is a genuine Unicode character between
every glyph. Selecting and copying a long serial number, licence number or ID number out of the
PDF carries those zero-width spaces along with it. Pasted into a plain text field this is
invisible; pasted into something that treats `U+200B` as a token boundary (some search boxes, some
spreadsheet cells doing exact-match lookups) it can silently break an exact-string match against
the same value typed by hand. This is an accepted cost, not an oversight: the alternative is the
overflow defect itself, which is a correctness problem on a register that is printed, signed and
inspected. If a future consumer needs the copy-paste text layer to be byte-identical to the typed
value, that requires either measuring column width per-document (this renderer does not do that -
column widths are only known after layout, whereas cell text is composed before layout) or
switching to a text layout approach with real subword break-anywhere support, not a change to this
threshold.

### Measured consequences

Rendering the same 16-column safe custody register data (30 rows, realistic long values - licence
numbers, 13-digit ID numbers, a long address, a long remark) with and without this fix, at
`BreakInterval = 3`:

- Page count: 8 pages with no fix, 10 pages with the fix - the same 10 pages as the original
  every-character version. Wrapping instead of overflowing makes cells taller when their long
  values wrap onto multiple lines, so rows grow and the document gets longer - this is the same
  tradeoff already accepted in item 10 above, now compounded by this fix. Spacing the break
  opportunities out to every 3 characters did not reduce the page count versus every character;
  it reduced render time and output size instead (below), because the number of *wrapped lines*
  a cell needs is governed by how much text has to fit, not by how many break opportunities are
  available once there are enough to reach the last one that fits.
- Output size (30-row document): 71,527 bytes with no fix, 83,767 bytes at every-character,
  80,654 bytes at every-3 characters.

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

Against that realistic fixture, render time for the 5000-row maximum export at each `BreakInterval`
tried:

| Insertion            | Render time (realistic 5000 rows) | Output size  |
|-----------------------|-----------------------------------|--------------|
| No fix (baseline)     | ~8.9s                              | 5,000,798 B  |
| Every character        | 20.1s - 22.8s (over budget on some runs) | 7,223,919 B |
| Every 3 characters (chosen) | ~11.0s - 11.8s (Release build); ~11.1s - 19.9s (Debug build, `dotnet test` default) | 6,705,121 B |
| Every 4 characters (rejected) | ~15.0s - 15.7s, worse than every 3 on every measure | 8,369,328 B |

Every-3 is a large, real improvement over every-character: roughly half the render time and about
1.5MB less output on a Release build. It comfortably clears the 20s budget in a Release build. In
a Debug build - which is what `dotnet test` runs by default, with no build configuration flag - the
same fixture varied from about 11s to just under 20s across repeated runs on this machine, with as
little as a few hundred milliseconds of headroom on the slowest observed run. The rewritten
performance test has not failed in repeated local runs, but the margin under the unchanged 20s
budget is thin and volatile in Debug configuration, and a slower or more loaded CI machine could
tip it over. This is reported here rather than papered over: the budget and the test were
deliberately left unchanged per instruction, so this is the honest number against them, not a
comfortable one.

Every-4 was tried and rejected: it was slower than every-3 on every measurement (5000-row render
time, output size, and page count on a 30-row check), not faster. Coarser break spacing gave
MigraDoc's line formatter fewer usable break points inside the narrowest columns, which produced
more wrapped lines overall rather than fewer - see "Why the interval is 3, not every character"
above.

## 12. The PDF export cap is 2000 rows, not 20000 - CSV carries the larger extracts

`ExportStorageRegisterQueryHandler.MaxPdfExportRows` is 2000. `MaxExportRows`, the CSV cap, is
20000 and is unaffected by anything in this document - CSV export does not go through MigraDoc and
has no comparable per-row rendering cost.

The two caps differ by 10x because rendering happens synchronously inside the HTTP request, on the
render lock described in item 6, and MigraDoc is substantially slower than the QuestPDF renderer it
replaced. Render time was measured against realistic 16-column safe custody register data (the same
fixture shape as `PdfSharpRegisterRendererPerformanceTests.RealisticRow`) across the full row range:

| Rows | Render time |
|------|-------------|
| 50   | 40 ms |
| 200  | 156 ms |
| 500  | 415 ms |
| 1000 | 756 ms |
| 2000 | 1.1 s |
| 5000 | 4.1 s on moderate data; 18 s - 19.9 s on content-heavy data (see item 11's "Measured consequences") |

The curve is roughly linear up to about 2000 rows and then worsens as more cells cross
`LongRunThreshold` and pick up `U+200B` break opportunities (item 11) - the 5000-row figure is not a
straight-line extrapolation of the smaller sizes, it is measurably worse per row. QuestPDF rendered
the same 5000-row load in about 1 second; MigraDoc does not stay inside a comfortable request budget
at that size, particularly on content-heavy data.

Capping the PDF export at 2000 rows keeps the worst case around a second, comfortably inside a
request, instead of the multi-second-to-20-second range 5000 rows produced. A user who needs more
than 2000 rows exports CSV instead - CSV has no MigraDoc cost and stays at the original 20000-row
cap. The validation error `ExportStorageRegisterQueryHandler` returns when a PDF request exceeds the
cap already tells the user to narrow the date range or export CSV for wider ranges.

Re-measured on this branch at the new 2000-row cap, against the same realistic fixture used by
`PdfSharpRegisterRendererPerformanceTests`, three consecutive Debug-build runs (2026-08-10,
`dotnet test`, which builds Debug by default and has been observed slower and more variable than
Release throughout this document): 2902 ms, 2930 ms, 3315 ms. The test's `BudgetSeconds` was set to
17, roughly five times the slowest of those three runs (3.315s x 5 = 16.575s, rounded up to 17),
giving real margin instead of the near-zero margin the old 5000-row test had at its 20s budget. If a
future measurement on this fixture regularly lands outside roughly the 3-4s range, re-derive the
budget the same way rather than assume this margin still holds.
