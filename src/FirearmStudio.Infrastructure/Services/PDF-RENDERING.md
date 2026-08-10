# Register PDF rendering (PDFsharp / MigraDoc)

This document explains the non-obvious decisions behind `PdfSharpRegisterRenderer`,
`RegisterTableLayout`, `RegisterCellText`, `RegisterTextMeasurer` and `EmbeddedFontResolver`.
Several of the choices here look like they could be simplified or reverted by someone unfamiliar
with MigraDoc's behaviour. Each one below is a real defect that was found and fixed, not a
hypothetical - read this before changing page setup, margins, fonts, column widths, cell text or
timestamps.

The renderer's source files carry no comments by design; this document is where the reasoning
lives. `Extensions/DependencyInjection.cs` is a pre-existing file and keeps its own comments.

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
TopMargin    = HeaderDistance + HeaderReservedHeight(document)
BottomMargin = FooterDistance + FooterReservedHeightPoints
```

`FooterReservedHeightPoints = 19` is still a constant, because the footer renders one line of
fixed shape (`Total rows: N` plus a page field). It was derived by rendering that footer, reading
the glyph baselines back out of the content stream, and adding the embedded Roboto font's
ascent/descent plus the layout's 8pt gap: ascent(Regular 8) 8.38pt + descent(Regular 8) 2.17pt +
8pt gap = ~18.55pt, rounded up to 19pt.

### The header reservation is measured per document, not a constant

An earlier version of this renderer used a constant `HeaderReservedHeightPoints = 91` here too,
sized for a six-line header in which **every line fits on one line**. That was a real defect, not
a theoretical one. `CompanyName` renders at 14pt bold and `CompanyAddress` is a composed string;
both are tenant-supplied and unbounded. A company name of roughly 113 to 116 characters or more
(measured against the 793.8898pt content width with `XGraphics.MeasureString` at 14pt bold; 113 for
a mixed-word name such as repeated `Bergview Arms and Ammunition Wholesalers`, 116 for repeated
`Bergview`) wraps to two lines, which pushes every line after it down
by one 14pt line height (18.4639pt) - far enough that the `Generated {date} (SAST) by {user}`
paragraph lands **inside the table's first row**.

MigraDoc does not clip the header frame, so the attribution line is still written to the content
stream. It is then **painted over**: the table's heading row carries a light-grey shading
rectangle, the table is drawn after the header, and the fill covers the text. Measured on a
four-column register with a one-row body (Release, standalone harness), before this fix:

| Company name length | `Generated` baseline y | Table top y | Visible? |
|---------------------|------------------------|-------------|----------|
| 20 to 113 chars     | 490.95                 | 479.78      | yes |
| 120 chars           | 472.49                 | 479.78      | no, covered by the heading-row fill |
| 260 chars           | 454.02                 | 479.78      | no |
| 400 chars           | 435.56                 | 479.78      | no |

**A test that only greps the content stream for `Generated` does not catch this**, because the
text is present in the stream in every row of that table. The regression test
(`A_company_name_that_wraps_keeps_the_generated_by_line_clear_of_the_table`) therefore parses the
content stream's text positioning and asserts the attribution line's baseline sits **above** the
first shading rectangle. It was confirmed to fail against a fixed 91pt reservation and pass
against the measured one.

The reservation is now derived per document in `HeaderReservedHeight`:

```
reserved = max(HeaderMinimumReservedHeightPoints,
               HeaderSafetyPadPoints + sum over header paragraphs of
                   (SpaceBefore + SpaceAfter + lineCount * lineHeight))
```

- `lineHeight` is `XFont.GetHeight()` for that paragraph's own size and weight. In PDFsharp this
  is exactly ascent + descent, so summing it over every line telescopes to the same figure the
  original hand-derivation produced from baseline offsets. For the standard six-line header:
  18.4639 (Bold 14) + 10.5508 + 10.5508 (Regular 8) + 15.8262 (Bold 12) + 10.5508 + 10.5508
  (Regular 8) + 6pt `SpaceBefore` on the title + 8pt `SpaceAfter` on the attribution line =
  **90.4933pt**, which reproduces the old hand-derived ~90.5pt to within 0.01pt. That agreement is
  the check that the measurement is modelling the right thing.
- `lineCount` is a greedy chunk-by-chunk wrap measured with `XGraphics.MeasureString` against the
  content width. **It must split on every character MigraDoc treats as a break opportunity, not
  just on spaces.** An earlier version of this measurement split on spaces only and argued that
  doing so could only ever over-count lines, which is safe. That argument is wrong, and the
  overprint above came straight back through the gap: a single space-free token wider than the
  793.8898pt content width counts as **one** line under a space-only split, while MigraDoc breaks it
  at its hyphens onto **two**. `CreateCompanyRequestValidator` allows `MaximumLength(200)` on the
  company name, so a hyphen-joined 200-character name arrives through the public onboarding
  endpoint. Measured on the space-only version, every hyphen-joined length from 116 to 200
  characters put the attribution baseline at 472.49 against a table top of 479.28 - covered, in
  every case.

  The split set was determined by rendering, not assumed. A 20-token name joined by each candidate
  character was rendered as the 14pt bold company line against the 793.8898pt content width, and the
  number of lines the paragraph occupied was read back out of the content stream:

  | Character | MigraDoc breaks? |
  |-----------|------------------|
  | space, `-` (U+002D), U+200B ZERO WIDTH SPACE, U+00AD SOFT HYPHEN | **yes** |
  | `/` `\` `.` `,` `_` `+` `=` `:` `;` `)` `]` `?` `!` `*` `&` `\|` U+2013 EN DASH | no |

  `LineCount` splits on exactly those four, keeping the break character attached to the chunk that
  precedes it so the measured chunk widths match what MigraDoc lays out, and trimming a trailing
  space before measuring because MigraDoc does not count one against a line. `U+200B` and `U+00AD`
  are included because `Sanitise` passes both through (neither is `char.IsWhiteSpace` nor
  `char.IsControl`), so a tenant can put either in a company name and MigraDoc will honour it.

  Slash is **not** in the set, and that is not an oversight - it is the same MigraDoc behaviour
  documented in item 11, where `RegisterCellText` has to insert an explicit `U+200B` after a slash
  precisely because MigraDoc will not break there. A slash-joined 200-character company name renders
  on one line, so a space-only split never under-counted it.

  Over-counting that survives this (a chunk MigraDoc would have fitted that this model pushes to the
  next line) is still safe: over-reservation costs a little body height, under-reservation brings the
  overprint back.
- `HeaderSafetyPadPoints = 1` absorbs rounding between this model and MigraDoc's own layout.
- `HeaderMinimumReservedHeightPoints = 91` is a floor, not a target. A document that omits the
  registration number and the address measures 70.39pt including the pad, and still gets 91pt, so
  a minimal header keeps exactly the spacing this renderer has always used and page counts for
  ordinary documents do not change.

Measuring needs an `XGraphics` without a real page: `XGraphics.CreateMeasureContext`.
`RegisterTextMeasurer` creates **one** measure context for the process, lazily, on the first
`Render` call, and reuses it. It must be created and used **inside the render lock** (item 6),
because it touches the same PDFsharp font machinery the lock exists to protect, and because its
caches are plain non-concurrent dictionaries. It is created lazily rather than in the static
constructor so that `GlobalFontSettings.FontResolver` (item 4) is guaranteed to be installed
before the first `XFont` exists.

`RegisterTextMeasurer` caches string widths keyed by (text, size, weight). That cache is cleared at
the start of every `Render` call. Registers repeat values heavily **within** one document, which is
where nearly all the benefit is, and clearing per render bounds the memory a long-lived singleton
would otherwise accumulate across every export the process ever serves. The `XFont` cache and the
measure context itself are not cleared.

If you change what the header renders (a new line, a different font size, different spacing), you
do not need to re-derive a constant any more - but you do need to add the new paragraph to
`HeaderParagraphs`, which is the single list both `ComposeHeader` and `HeaderReservedHeight` read.
Do not compose a header paragraph anywhere else; a paragraph that is drawn but not measured is
exactly the defect above.

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
cell content, so it does not reflect what the shipped renderer actually produces.

Page count depends entirely on which dataset produced it, so there is no single figure - two
datasets are given below, both re-measured against the **shipped width-aware break insertion**
(item 11, `BreakInterval = 3`), Release configuration, standalone harness, and it matters which one
you are looking at.

**The realistic case.** A dataset over the register's real 16 safe custody columns
(`SafeCustodyRegisterCsvBuilder.Headers` plus the trailing `Signature` column): dates in
`Date Received`/`Date Returned`, makes such as `CZ` and `Glock` in `Make`, models such as
`Shadow 2` and `P320`, calibres such as `9mm`, serial numbers such as `SN0000-XY00`, a full name in
`Licence Holder`, a 13-digit `ID / Reg No`, a short `Address` with **one row in twenty** carrying a
wrapping street address, licence numbers such as `WC/2020/00000`, `SAFE00`/`R00` in
`Safe Number`/`Rack Number`, `Strongroom A` in `Storage Location`, `Released` or `InStorage` in
`Storage Status`, and a blank `Signature` column - long values in roughly four of the sixteen
columns, not all of them:

| Rows | Pages (width-aware) | Rows/page | Output size | Pages (previous character-count rule) |
|------|---------------------|-----------|-------------|----------------------------------------|
| 60   | 5                   | 12.0      | 76.8 KB     | 5 |
| 200  | 15                  | 13.3      | 158.4 KB    | 15 |
| 2000 | 143                 | 14.0      | 1206.3 KB   | 150 |

Rows/page is the figure that actually transfers between dataset sizes - it is set by the tallest
wrapping cell in each row rather than by row count, which is why it barely moves across a 33x range
in row count. The small drift from 12.0 to 14.0 is the one-in-twenty long address rows averaging
out over larger samples, not a row-count effect.

An earlier version of this table reported 6/20/200 pages at a flat 10.0 rows/page for a fixture it
also called "realistic". That fixture is not the one above: it carried a heavier address and a
free-text remark, and its figures were measured before the width-aware rule existed. The figures
above supersede it. The previous-rule column is measured on the **same** dataset with the same
harness, so it is a like-for-like comparison of the two break rules and nothing else.

**Break opportunities did not increase the page count on this dataset - the width-aware rule
lowered it.** 2000 rows went from 150 pages under the previous character-count rule to 143 under
the shipped width-aware one, because values that always fitted their column (`Released`, `SAFE00`,
every date, every column heading) no longer pick up a mid-word break and no longer force an extra
line. A reader who has only seen the worst-case figure below could otherwise conclude this work
made every printed register three times longer - that is false for realistic data, and this line
exists so nobody draws that conclusion from the wrong fixture.

**The worst case.** `PdfSharpRegisterRendererPerformanceTests.RealisticRow` is **not** a real
register shape and its column names are **not** the register's real columns. It is a synthetic
16-column fixture (`Type`, `Owner Name`, `Purpose`, `Condition`, `Received From`, `Remarks` and so
on) that deliberately stacks a long value into essentially every column, which no real safe custody
row looks like. It exists on purpose, as a pessimistic guard: the performance test in item 12 uses
it so that the render-time budget is tested against a genuinely worst-case row shape, not an
average one, and this document uses the same shape elsewhere (items 11 and 12) so that its
render-time and byte-count figures describe a guaranteed upper bound rather than a typical case.
**Wherever this document names a column, check whether it is quoting this synthetic fixture or the
real register** - the real column orders are `SafeCustodyRegisterCsvBuilder.Headers` plus a trailing
`Signature` column (16 columns) and `FirearmsRegisterCsvBuilder.Headers` (15 columns), and neither
of them has a `Type`, `Owner Name`, `Purpose` or `Remarks` column in the position this fixture puts
it.

At 2000 rows the pessimistic fixture renders to 667 pages, 2,516,968 bytes - 3.0 rows/page, about
a fifth of the realistic case's density (Release, standalone harness, shipped width-aware rule).
Under the previous character-count rule the same fixture rendered to the same 667 pages but
2,729,605 bytes, so the width-aware rule takes about 7.8% off the output size here without changing
the page count. An earlier version of this document mislabelled a second attempt at a lighter
fixture as "moderate" and reported it also landing at 667 pages, concluding that trimming column
content didn't matter; that fixture was itself still pessimistic and the "moderate versus
pessimistic tie" conclusion drawn from it has been removed as a result - it was an artefact of
comparing two worst-case fixtures against each other, not a real finding about typical data.

For contrast, a fixture with genuinely trivial values in every column (4-character strings like
`r0c0`, the shape the performance test's fixture used before it was rewritten) renders the same
2000 rows to only 141 pages, about 14.2 rows/page - confirming again that the driver is which
specific fields wrap and by how much, not row count.

This document no longer states a QuestPDF page-count comparison. QuestPDF was removed from the
codebase during this migration, so it cannot be re-rendered to check, and the old "roughly 179
pages" figure was measured at 5000 rows, a size this renderer's PDF export can no longer even
request (item 12) - it is not comparable to any figure above at any row count that still applies.
Item 12 keeps a QuestPDF *render time* comparison (about 1 second for 5000 rows) because that is an
independent historical fact about QuestPDF's own speed, not tied to the row cap that has since
changed on this side; no equivalent page-count fact is retained here because none can be verified
at current settings.

If you need to reason about a future dataset's page count, measure it directly against the shipped
width-aware break rule and `BreakInterval`, with a fixture that actually matches how many columns
carry long values in production, rather than assume either figure above - the realistic and worst-case
fixtures differing by more than 3x on rows/page shows that how many columns are long matters far
more than how long any single value is.

## 11. Long unbroken cell values overflow their column unless given a U+200B break opportunity

QuestPDF broke long tokens at the character level and kept them inside their cell. MigraDoc has
no equivalent "break anywhere" setting - a long unbroken run of characters (a licence number, an
ID number, a single long word) is laid out as one unbreakable unit and simply overprints whatever
column comes next once it runs past the cell's right edge. This is not cosmetic: on the 16-column
safe custody register at 8pt, the licence number `WC/2020/00000` runs 9.0pt out of `Licence Number`
into its real neighbour `Licence Issued` (measured by rendering), and a wide value in `Calibre`
runs into its real neighbour `Serial Number` - `MMMMM` overhangs by 3.3pt. Long free text in
`Address` was reported doing the same into `Licence Number`. There is no exception and no warning -
the content stream is valid, the PDF opens fine, and the defect is only visible on inspection.

(The real column orders are `SafeCustodyRegisterCsvBuilder.Headers` plus a trailing `Signature`
column - `Date Received`, `Date Returned`, `Make`, `Model`, `Calibre`, `Serial Number`,
`Licence Holder`, `ID / Reg No`, `Address`, `Licence Number`, `Licence Issued`, `Safe Number`,
`Rack Number`, `Storage Location`, `Storage Status`, `Signature` - and
`FirearmsRegisterCsvBuilder.Headers` - `Internal Ref`, `Type`, `Make`, `Model`, `Calibre`,
`Serial Number`, `Owner Name`, `Owner ID / Reg No`, `Owner Address`, `Licence Number`,
`Licence Issued`, `Licence Expires`, `Date Received`, `Date Returned`, `Firearm Status`. An earlier
version of this document described `Type`, `Owner Name` and `Purpose` as safe custody columns and
claimed `Calibre` follows `Type`; those names came from the synthetic performance-test fixture, not
from either real register, and have been corrected throughout.)

MigraDoc does honour `U+200B` (ZERO WIDTH SPACE) as a line-break opportunity, the same way it
treats an ordinary space or a hyphen. `RegisterCellText.InsertBreakOpportunities` uses that to
keep a cell's content inside its column.

### The rule is width aware, not character-count based

**MigraDoc breaks a line at the *last* opportunity that fits.** An interior `U+200B` always comes
later in the string than the preceding space, so inserting break opportunities into a value that
would have fitted anyway makes MigraDoc choose the mid-word break in preference to the space. An
earlier version of this fix inserted a `U+200B` every `BreakInterval` characters into **any** run
longer than a fixed `LongRunThreshold = 5`, regardless of whether that run would have fitted its
column. The rendered production register showed exactly that failure. Heading row, before:

```
Date | Rec | eiv | ed || Date | Ret | urn | ed || Cal | ibr | e || Ser | ial | Num | ber
Lic | enc | e | Hol | der || Safe | Num | ber || Rack | Num | ber || Sto | rag | e | Loc | ati | on
Sig | nat | ure
```

and data cells rendered `Releas/ed`, `7.62x5/1`, `SAF/E00`, and every single date as `202` `6-`
`0` `1-` `0` `1`. QuestPDF broke at spaces and hyphens and looked correct.

The shipped rule takes the width the column actually has and measures against it:

1. `PdfSharpRegisterRenderer.ComposeContent` already knows every column's width from
   `RegisterTableLayout.ColumnWidths`. For each column it computes a **break width** and passes it
   into `InsertBreakOpportunities` along with a measuring delegate.
2. Each whitespace-delimited run is measured first. **A run that fits the break width is returned
   completely untouched** - no `U+200B` anywhere - so MigraDoc falls back to breaking at the
   surrounding spaces, exactly as QuestPDF did.
3. A run that does not fit is split at `-` and `/` first, and each segment is measured separately.
   `2026-01-01` splits into `2026-`, `01-`, `01`, all of which fit, so the value receives no
   character-level break and wraps at a hyphen. `WC/2020/00000` splits into `WC/`, `2020/`, `00000`,
   all of which fit, so it wraps at a slash.
4. Only a segment that still does not fit is chunked with a `U+200B` every `BreakInterval`
   characters.

A `U+200B` is emitted after every segment boundary of a run that did not fit, including after a
slash. That is not redundant: **MigraDoc breaks natively at a hyphen but not at a slash.** Verified
by rendering - `2026-01-01` is emitted as three separate `Tj` operators with no help from this
code, while `2015/098765/07` in the page header (which this code never touches) is emitted as a
single unbroken run. Without an inserted opportunity after the slash, `WC/2020/00000` overhangs its
column by 9.0pt on the safe custody register and 4.1pt on the firearms register. Do not "simplify"
the slash handling away on the assumption that MigraDoc treats `/` like `-`.

### Why the break width is the column border, not the text box

There are two different widths in play for a cell, and they differ by more than 6pt in the
narrowest column:

```
column width                                 38.4887pt  (safe custody, weight 0.8 of 16.6)
- left border 0.5 + indent 3 + padding 3.4016 = 6.9016
- right indent 3 + padding 3.4016             = 6.4016
= usable text width (MigraDoc's line width)  25.1855pt
= break width (this renderer's threshold)    31.5871pt
```

The **usable text width**, `column width - 0.5 - 2 * (3 + 3.4016) = 25.1855pt`, is the width
MigraDoc lays lines out against. `AddCellText`'s caller sets a 3pt `LeftIndent`/`RightIndent` on
each `Column`, MigraDoc applies its own default cell padding of 0.12cm (3.4016pt) on top of that,
and the table's 0.5pt `BorderWidth` consumes the rest. Confirmed by rendering: the first column's
text origin sits at 30.9016 against a content-box left edge of 24.0, exactly 6.9016 in. The observed
wrap points bracket the derived figure from both sides. Read off the *previous* rendering, whose
character-count rule chunked the column headings and so exposed where each line actually ended: the
`Serial Number` column at 52.5469pt wide put `Serial` (20.691pt) on one line but pushed `Number` to
the next, so `Serial Num` at 39.617pt did not fit and its chrome is above 12.93; the
`Storage Location` column at 57.233pt kept `Storage Loc` at about 43.43pt on one line, so its chrome
is at most 13.80. The derived 13.3032 sits inside that (12.93, 13.80] bracket, which is a stronger
confirmation than the padding arithmetic alone because it is a directly observed wrap point.

The **break width**, `column width - 6.9016 = 31.5871pt`, is the point at which text leaving the
text box would actually reach the vertical rule and enter the neighbouring column. It is larger
than the usable text width because the cell's own right-side indent and padding sit between the
text box and the border and provide real slack.

**Break insertion triggers on the break width, not the usable text width.** That is a deliberate
choice and it is the difference between a readable register and a mangled one:

- The defect being fixed is text **crossing into the neighbouring column**. Text that leaves its
  text box but stops inside its own right-hand padding does not collide with the neighbouring cell's
  text. It is not free of visual cost, though - see "The narrowest column is tight for 8pt text"
  below.
- Several real column headings sit between the two figures. `Number` measures 28.797pt in bold at
  8pt: wider than the 25.1855pt text box of the `Safe Number` and `Rack Number` columns, narrower
  than their 31.5871pt break width. Triggering on the text box would have rendered `Safe` / `Num` /
  `ber` on three lines; triggering on the break width renders `Safe` / `Number` on two, with
  `Number` running 3.6pt into its own padding and its advance box stopping about 2.8pt short of the
  rule. The same
  applies to `Calibre` (25.512pt bold, against a 25.1855pt text box), `Signature` (34.797pt bold
  against 34.5576pt), `Received` (32.922pt against 29.8715pt) and `Returned`.
- Nothing is left unguarded by the looser threshold. Verified by rendering (see "Verified by
  rendered geometry" below) rather than argued.

Do not try to reclaim width by reducing `CellPadding` or overriding MigraDoc's default cell
padding - either changes the visual density of every column in the register, which is a separate,
out-of-scope decision.

### The narrowest column is tight for 8pt text, and that is a weights problem

Choosing the break width over the text box means a value between the two renders into its own
right-hand gutter. On headings the tightest case is `Number`, whose advance box stops about 2.8pt short of the rule -
comfortable. **On realistic body data it gets much tighter than that, and
the earlier claim that such text "collides with nothing" understated it.**

The tightest realistic value measured is the calibre `9x19mm` at 31.465pt in the 38.4886pt `Calibre`
column (8pt regular, embedded Roboto). Its text origin sits 6.9016pt inside the column, so its
advance box ends 38.366pt in, leaving **0.122pt to the column edge** - and since the 0.5pt rule is
centred on that edge, the advance box actually reaches 0.128pt past the rule's near side. The last
*ink* stops earlier than the advance box does, by the trailing side bearing of `m`, which leaves
roughly **0.74pt of white** between the final stroke and the rule. That is about the value's own
inter-letter spacing, and at raster resolution it reads as the word touching the line.

`9x19mm` is not alone, only the worst: the make `Mossberg` at 35.719pt in the 43.1747pt `Make`
column (weight 0.9) leaves 0.554pt, and the firearm type `Handgun` at 32.293pt in the firearms
register's 41.7263pt `Type` column leaves 2.532pt. The pattern is the 0.8 and 0.9 weight columns,
not one specific value.

It is legible and it does not overlap the neighbouring cell's text, so it is not a correctness
defect and it is not what this work was scoped to fix. But state the cause plainly rather than
leaving it to be rediscovered: **the `Calibre` column carries weight 0.8 of 16.6 across 16 columns,
which is simply narrow for 8pt text.** That is a column-weights decision in
`RegisterDocumentFactory.SafeCustodyColumnWeights`, not a break-threshold decision, and nothing in
`RegisterCellText` can fix it - lowering the break width to the 25.1855pt text box would only wrap
`9x19mm` into `9x1` / `9mm`, which is worse. If the tightness matters, raise the `Calibre` weight
(and correspondingly lower a wide one such as `Address` at 1.8) or drop the body font below 8pt.
Both are out of scope here and both change the whole register's appearance.

### `LongRunThreshold` has been removed

The old `LongRunThreshold = 5` was a character count standing in for a width, and this document
previously admitted what that cost: "a wide enough 5-character run can still exceed the text box
and cross the column border - this is a content-dependent guarantee, not a geometric one".
Measured in embedded Roboto 8pt, `MMMMM` is 34.922pt, `WWWWW` 35.488pt and `@@@@@` 35.918pt, all
past the 31.5871pt break width of the narrowest safe custody column, and a rendered register
containing them crossed a real column border **9 times** (three values, in `Calibre`,
`Safe Number` and `Rack Number`, overhanging by 3.3 to 4.3pt each).

Direct measurement supersedes the threshold entirely, so it has been deleted rather than kept as a
cheap pre-filter:

- It is not needed for correctness. Measurement catches the wide 5-character runs it missed.
- Keeping it would keep a second, layout-dependent constant that has to be re-derived every time a
  weight, a column count or the page geometry changes - which is precisely the class of defect this
  change removes. It had already been re-derived twice on this branch (6, then 5).
- It is not needed for speed. `RegisterTextMeasurer` caches widths by string, and registers repeat
  values heavily, so the measurement is a dictionary hit for nearly every cell. Render time for the
  2000-row worst-case fixture is unchanged within run-to-run noise: 1716/1743/1737 ms before,
  1882/1761/1790 ms after (Release, standalone harness, three consecutive runs each).

Deleting it did not regress anything: the full suite passes, and the rendered-geometry check below
reports zero border crossings where the old rule reported nine.

### Why the interval is 3, not every character

`BreakInterval = 3` is unchanged by the width-aware work and its original justification still
holds. The first version of this fix inserted `U+200B` between every character of a broken run.
That is much stronger than the defect requires: MigraDoc breaks a line at the *last* opportunity
that still fits, so an opportunity every 3 characters is enough to prevent overflow, and a cell
that fits `N` characters simply breaks at the nearest opportunity at or before the edge with no
overhang. What every-character insertion bought beyond that was tighter packing at a real cost:
three times as many inserted characters means three times as many distinct text-showing operations
for MigraDoc's layout engine to measure and position, which dominated render time at scale.

Interval 4 was tried and rejected - it did not continue the improvement. Coarser breaks give
MigraDoc's line formatter fewer places to end a line inside a narrow column, and in this data that
pushed some lines mid-chunk past where a finer break would have fit, producing *more* wrapped
lines, not fewer, which made both page count and render time worse than interval 3. Do not raise
`BreakInterval` above 3 without re-measuring on realistic data the same way - "coarser" does not
reliably mean "cheaper" once a chunk stops fitting cleanly against a narrow column's edge.

**Interval 3 stays safe, but the margin is not structural.** The widest realistic 3-character
chunks in embedded Roboto 8pt are `@@@` at 21.551pt, `WWW` at 21.293pt and `MMM` at 20.953pt, all
comfortably under both the 25.1855pt usable text width and the 31.5871pt break width of the
narrowest safe custody column. That margin exists only because these figures were derived against
*this* register's actual narrowest column. `RegisterTableLayout` guarantees only a 1pt-per-column
floor (item 7) - it makes no promise about how narrow the narrowest weighted column can get. A
future register with more columns, a smaller minimum weight, or a near-zero weight column could
produce a column narrow enough that a 3-character chunk no longer fits, and no amount of
measurement in `InsertBreakOpportunities` will help, because a chunk is the smallest unit it emits.
Re-run the measurement before assuming interval 3 is still safe for a materially different layout.

### Which register binds the derivation, and it is not obvious

The safe custody register is the binding case, and this had never been checked before:

| Register     | Columns | Weight sum | Min weight | Narrowest column | Usable text width | Break width |
|--------------|---------|------------|------------|------------------|-------------------|-------------|
| Safe custody | 16      | 16.6       | 0.8        | 38.4887pt        | 25.1855pt         | 31.5871pt   |
| Firearms     | 15      | 15.3       | 0.8        | 41.7263pt        | 28.4231pt         | 34.8247pt   |

Both registers have three columns at the minimum weight of 0.8: `Calibre`, `Safe Number` and
`Rack Number` in safe custody, and `Internal Ref`, `Type` and `Calibre` in firearms. Safe custody
is narrower because it splits a fixed content width across one more column against a larger weight
sum, so every figure in this section is derived from it and the firearms register has 3.24pt more
slack in its narrowest column.

That ordering is not permanent. It follows from
`RegisterDocumentFactory.SafeCustodyColumnWeights` and `FirearmsColumnWeights`, and a future weight
change to **either** register could invert which one binds - lowering a firearms weight below 0.8,
raising the firearms weight sum, or widening safe custody's narrowest columns would all do it. If
you change either weight array, recompute both rows of the table above before assuming the safe
custody figures still bound the firearms register.

### Verified by rendered geometry, not only by tests

The width-aware rule was verified by rendering both registers with realistic 60-row data through
`RegisterDocumentFactory`'s real column sets, decompressing every page's content stream, tracking
the text matrix through `BT`/`Td`/`Tj`, measuring each drawn run with `XGraphics.MeasureString`
against the same embedded font, and comparing its right-hand extent to the computed column
boundaries (Release, standalone harness):

| Document                                        | Text runs, previous / shipped | Border crossings, previous | Border crossings, shipped |
|-------------------------------------------------|-------------------------------|-----------------------------|----------------------------|
| Safe custody, 60 realistic rows                 | 4916 / 2876                   | 0                           | 0 |
| Firearms, 60 realistic rows                     | 4926 / 2928                   | 0                           | 0 |
| Safe custody, adversarial values in all columns | 337 / 266                     | 9                           | 0 |

The two realistic documents show zero crossings under both rules because their long values
(`WC/2020/00000`, a 13-digit ID number) were caught by both. The adversarial document is where the
character-count rule fails, and it fails silently.

The adversarial document puts `MMMMM`, `WWWWW`, `@@@@@`, `000000`, `Handgun`, `MMMMMM`,
`WWWWWWWW`, `Rifle` and `SMITH` in every one of the 16 columns. Under the shipped rule, in the
narrowest column: `MMMMM` breaks to `MMM` + `MM`, `WWWWW` to `WWW` + `WW`, `@@@@@` to `@@@` + `@@`,
`Handgun` to `Han` + `dgun`, while `000000` (26.953pt), `Rifle` (15.824pt) and `SMITH` (24.380pt)
stay intact because they fit the 31.5871pt break width. `Handgun` is the closest call at 32.293pt,
0.7pt over.

The same rendering confirms the heading row now reads:

```
Date | Received || Date | Returned || Make || Model || Calibre || Serial | Number
Licence | Holder || ID | / | Reg | No || Address || Licence | Number || Licence | Issued
Safe | Number || Rack | Number || Storage | Location || Storage | Status || Signature
```

and that a date cell renders as `2026-` / `01-` / `01` rather than `202` / `6-` / `0` / `1-` /
`0` / `1`.

A side effect worth knowing: the number of text-showing operators on the same document dropped from
4916 to 2876, because most values are no longer split into 3-character chunks. That directly
improves the text-extraction problem described under "Accepted cost" below - there is simply much
less chunking left to reconstruct.

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
through unchanged and the two functions never fight over the same characters. That last claim used
to be asserted only in this prose; it is now pinned by
`RegisterCellTextTests.Sanitise_passes_a_break_opportunity_through_unchanged`.

### Accepted cost: the text layer is chunked, not annotated with zero-width spaces

An earlier version of this document claimed that a genuine `U+200B` sits between glyphs in the
rendered PDF's text layer, and that copying text out carries those characters along. **That claim
is not true, and has been corrected.** A geometric review scanned the per-page decompressed content
streams and the whole file via `qpdf --qdf`, checking for `U+200B` encoded as UTF-8 (`e2 80 8b`),
UTF-16BE (`20 0b`) and the literal ASCII string `200B`, and found zero occurrences anywhere in a
rendered PDF. MigraDoc consumes `U+200B` purely as a line-breaking instruction during layout - it
decides where to end each line and never writes the character itself into the output. Confirmed
also in this document's own round 2 fix verification: a geometric check that parsed a rendered
page's content stream found a `000000` example in the narrowest column emitted as two separate `Tj`
operators, `(000) Tj` and `(000) Tj`, not one `(000` + `U+200B` + `000) Tj` run - there is no
zero-width space character anywhere in the string PDFsharp writes. (Under the shipped width-aware
rule `000000` no longer gets broken in that column at all - it measures 26.953pt against a
31.5871pt break width - but the observation about how MigraDoc consumes `U+200B` is unchanged for
values that are still broken.)

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
printed, signed and inspected. The width-aware rule reduces how much text this affects by a large
margin - most values are no longer chunked at all, and the operator count on a 60-row safe custody
register fell from 4916 to 2876 - but it does not eliminate it for values that genuinely do not fit,
such as a 13-digit ID number in a narrow column. If a future consumer needs extracted or copied text
to be byte-identical to the typed value, that requires either embedding a `/ToUnicode` CMap that
maps chunk boundaries back to the original unbroken string, or switching to a text layout approach
with real subword break-anywhere support.

**This has not been verified against a real PDF viewer's copy-paste behaviour.** Neither
`pdftotext` nor `mutool` was available in the environment used to derive the above - the claim
about `Han dgun`-style reconstruction is inferred from the content stream structure (separate `Tj`
operators, no `/ToUnicode` CMap), not observed by actually selecting text in a viewer and reading
the clipboard. Before relying on this section for anything user-facing, someone should open a
rendered register in a real PDF viewer, select and copy a licence number and an ID number that
were chunked, and confirm what actually reaches the clipboard.

### Measured consequences

Rendering the same 30-row document (the synthetic pessimistic fixture shape - licence numbers,
13-digit ID numbers, a long address, a long remark in most of the 16 columns; the same shape as
`PdfSharpRegisterRendererPerformanceTests.RealisticRow`, whose column names are **not** the real
register's, see item 10) with each break rule in turn, Release configuration, this document's own
standalone harness:

| Break rule                                       | Pages | Output size |
|--------------------------------------------------|-------|-------------|
| No break opportunities at all                    | 8     | 71,527 B    |
| Character count (`LongRunThreshold = 5`, interval 3) | 10 | 81,023 B    |
| Width aware (shipped, interval 3)                | 10    | 77,850 B    |

- Page count: 8 pages with no fix, 10 with either break rule. Wrapping instead of overflowing makes
  cells taller when long values wrap onto multiple lines, so rows grow and the document gets longer
  - this is the same tradeoff already accepted in item 10 above. On this deliberately pessimistic
  fixture nearly every column carries a value that genuinely does not fit, so the width-aware rule
  has little to leave alone and does not recover a page here; on realistic data it does (item 10).
- The 81,023-byte figure for the superseded character-count rule was re-measured with this harness
  against this same document, so the 3,173-byte (3.9%) saving is a like-for-like comparison of the
  two rules. An earlier version of this document recorded 80,654 bytes for that rule measured at the
  even older `LongRunThreshold = 6`, and a separate review's harness measured 80,755 bytes; both are
  within 0.5% of 81,023 and are consistent, not drift.
- A third figure sometimes cited alongside these, 83,767 bytes for "every character"
  (`BreakInterval = 1`), was also measured at the old threshold 6 and describes an interval rejected
  in an earlier round (see "Why the interval is 3, not every character" above). It is historical
  illustration of why every-character insertion was abandoned, not a claim about current output, and
  has not been re-measured because the shipped code no longer has an every-character code path.

**The originally committed performance test's fixture understated the true cost of this renderer
by roughly a factor of two and a half, independent of this fix.** Its rows use values like `r0c0`
(4 characters in every column, comfortably inside even the narrowest column's break width), which is
not what a real safe custody register looks like - real rows carry a full owner name, a wrapping street address and a
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

Render time for a 5000-row export at each `BreakInterval` tried, under the superseded
character-count rule at `LongRunThreshold = 6` (the rule in force at the time; it was later
corrected to 5 and has since been replaced entirely by the width-aware rule above). Changing which
runs get broken does not change the relative cost ordering between intervals, so this comparison
remains valid for choosing `BreakInterval`:

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

Both constants are `internal` rather than `private`, with `InternalsVisibleTo` on
`FirearmStudio.Application` for both test assemblies, so that
`ExportStorageRegisterQueryHandlerTests` and `PdfSharpRegisterRendererPerformanceTests` reference
the cap instead of duplicating the literal `2000`. The cap decision itself lives in
`ExportStorageRegisterQueryHandler.RowCapError`, split out of `Handle` for exactly one reason: the
cap is a user-visible contract (the error code and the row count in the message) and it could not
otherwise be covered without an EF Core in-memory provider this repository does not reference. The
tests assert both caps, both messages, and that a count exactly at the cap is accepted.

The two caps differ by 10x because rendering happens synchronously inside the HTTP request, on the
render lock described in item 6, and MigraDoc is substantially slower than the QuestPDF renderer it
replaced. Render time was measured against the synthetic pessimistic 16-column fixture (the same
shape as `PdfSharpRegisterRendererPerformanceTests.RealisticRow`, whose column names are not the
real register's - see item 10) across the full row range. Every timing below states its build
configuration explicitly - Release and Debug differ by roughly 2-4x on this renderer, so a number
without a stated configuration is not comparable to anything else in this document:

| Rows | Render time | Break rule | Build configuration |
|------|-------------|------------|----------------------|
| 50   | ~60 ms      | character count | Release (standalone harness) |
| 200  | ~250-300 ms | character count | Release (standalone harness) |
| 500  | ~665-685 ms | character count | Release (standalone harness) |
| 1000 | ~850 ms-1.1 s | character count | Release (standalone harness) |
| 2000 | 1716/1743/1737 ms | character count | Release (standalone harness) |
| 2000 | 1882/1761/1790 ms | width aware (shipped) | Release (standalone harness) |
| 2000 | 2738/2898/3260 ms | width aware (shipped) | Debug (`dotnet test`, no build flag) |
| 5000 | ~9.9-12.1 s | character count | Release (standalone harness) |

The two 2000-row Release rows are three consecutive runs each, measured back to back on the same
machine, and they overlap: **the width-aware rule did not change render time measurably.** It
measures more strings than the character-count rule skipped, but `RegisterTextMeasurer` caches
widths by string and a register repeats values heavily, so the extra work is dominated by MigraDoc's
own layout cost either way.

The curve is roughly linear up to about 2000 rows and then worsens somewhat at 5000 as more cells
need break opportunities (item 11) - the 5000-row figure is not a pure straight-line extrapolation
of the smaller sizes. QuestPDF rendered a 5000-row load in
about 1 second; MigraDoc does not stay inside a comfortable request budget at that size on a Debug
build, and even in Release it is an order of magnitude slower than QuestPDF was.

Capping the PDF export at 2000 rows keeps the worst case in the low single-digit seconds even in
Debug configuration, comfortably inside a request, instead of the multi-second-to-tens-of-seconds
range 5000 rows produced depending on build configuration and content. A user who needs more than
2000 rows exports CSV instead - CSV has no MigraDoc cost and stays at the original 20000-row cap.
The validation error `ExportStorageRegisterQueryHandler` returns when a PDF request exceeds the cap
already tells the user to narrow the date range or export CSV for wider ranges.

`BudgetSeconds` is 10, re-derived twice on this branch as the row cap and the break rule changed.
Three consecutive Debug-build runs against the current 2000-row cap and the shipped width-aware rule
(2026-08-10, `dotnet test`, which builds Debug by default and has been observed slower and more
variable than Release throughout this document): 2738 ms, 2898 ms, 3260 ms. The immediately
preceding character-count rule measured 2896/2945/2868 ms the same way, so the two rules are
indistinguishable at this size once Debug variance is accounted for. At a prior 17s budget, set against an earlier three-run measurement of 2902-3315ms, the
guard had roughly 5x headroom over the slowest observed run - comfortable, but wide enough that a
2x, 3x or even 4x regression (the same order of magnitude this MigraDoc migration itself produced
over QuestPDF) would not have tripped it. `BudgetSeconds = 10` keeps about 3x headroom over the
slowest Debug run observed here (3260ms x 3 is under 10s) while restoring real sensitivity to a
2x-4x regression. If a future measurement on this
fixture regularly lands outside roughly the 2.8-3.5s range in Debug configuration, re-derive the
budget the same way rather than assume this margin still holds.

## 13. The production container runs globalization-invariant and ships no ICU

The runtime image is `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` - the plain chiseled
base, not the `-extra` variant. That base ships **no ICU** and sets
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` in its own image config, so the app runs in invariant
globalization mode in production even though nothing in this repository sets that variable.

Nothing is broken by this today and nothing needs changing. It is recorded here because it is a
non-obvious consequence of narrowing the base image, it is invisible in the Dockerfile itself, and
it fails at runtime rather than at build time:

- **Constructing a specific culture will throw.** `new CultureInfo("en-ZA")` (or any named culture)
  raises `CultureNotFoundException` under invariant globalization. Every format call in this
  codebase currently passes `CultureInfo.InvariantCulture` or uses a culture-independent format
  such as `yyyy-MM-dd`, so no code path hits this - but the first person who reaches for a named
  culture to format a rand amount or a South African date will find it works locally and throws in
  production.
- **Culture-sensitive comparison and casing degrade to ordinal.** String comparisons, sorting and
  `ToUpper`/`ToLower` without an explicit culture behave ordinally under invariant mode. This
  matters for anything that sorts names for display.
- **The time zone database is copied in deliberately.** The Dockerfile's
  `COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo` is not incidental: the chiseled base
  ships no tzdata, and `SouthAfricaTimeZone` resolves `Africa/Johannesburg` at runtime. Without
  that copy every register export throws `TimeZoneNotFoundException` on the
  `TimeZoneInfo.ConvertTimeToUtc` call in item 9. Invariant globalization does **not** disable time
  zone lookups - they read tzdata, not ICU - so the two are independent requirements and removing
  either one breaks a different thing.
- TLS certificate validation is unaffected: the chiseled base ships a CA bundle, so outbound HTTPS
  (Klaviyo, Sage) still works.

If a future requirement genuinely needs a named culture, the fix is to switch the runtime image to
the `-extra` chiseled variant (which includes ICU) or to set
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` **and** add the ICU packages - not to catch the
exception at the call site.
