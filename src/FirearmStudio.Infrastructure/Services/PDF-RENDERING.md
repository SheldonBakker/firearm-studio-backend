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

Serialising every export process-wide is an accepted cost: the largest register (5000 rows)
renders in a few seconds, and register PDF exports are infrequent admin operations, not a hot
request path. Do not remove or narrow this lock without re-running a high-concurrency corruption
test of the kind described above, comparing byte length and content-stream hashes across many
parallel renders of the same document - the failure mode does not throw, so ordinary test
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
