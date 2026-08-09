namespace FirearmStudio.Infrastructure.Services;

/// <summary>
/// Turns the register's relative column weights into the absolute column widths MigraDoc needs.
/// MigraDoc does not scale a table to fit: widths that overshoot the content box push columns
/// silently off the page, so the result always sums to exactly the content width.
/// Every column is guaranteed at least MinimumWidth whenever the content width allows it.
/// A weight list that is null, shorter than the column count, contains any negative value,
/// or sums to zero falls back to equal widths for all columns.
/// </summary>
internal static class RegisterTableLayout
{
    // Enough to still draw a cell border when a caller supplies a zero weight.
    private const double MinimumWidth = 1d;

    public static double[] ColumnWidths(int columnCount, IReadOnlyList<float>? weights, double contentWidth)
    {
        if (columnCount <= 0)
        {
            return [];
        }

        var widths = new double[columnCount];
        var assigned = 0d;

        var floorBudget = columnCount * MinimumWidth;

        if (contentWidth <= floorBudget)
        {
            // Too little room to honour the floor and the exact sum at once. The sum wins:
            // a table that overflows the page is a worse failure than a hairline column.
            for (var i = 0; i < columnCount - 1; i++)
            {
                widths[i] = contentWidth / columnCount;
                assigned += widths[i];
            }

            widths[columnCount - 1] = contentWidth - assigned;
            return widths;
        }

        // Reserve the floor for every column first, then split only what is left over by
        // weight. This keeps every column at or above MinimumWidth without the floor ever
        // pushing the running total past the content width.
        var effective = EffectiveWeights(columnCount, weights);
        var total = effective.Sum();
        var distributable = contentWidth - floorBudget;

        // The last column takes the remainder so rounding drift can never overflow the page.
        for (var i = 0; i < columnCount - 1; i++)
        {
            widths[i] = MinimumWidth + (distributable * effective[i] / total);
            assigned += widths[i];
        }

        widths[columnCount - 1] = contentWidth - assigned;
        return widths;
    }

    private static double[] EffectiveWeights(int columnCount, IReadOnlyList<float>? weights)
    {
        var usable = weights is not null
            && weights.Count >= columnCount
            && weights.Take(columnCount).All(w => w >= 0f)
            && weights.Take(columnCount).Sum() > 0f;

        if (!usable)
        {
            return [.. Enumerable.Repeat(1d, columnCount)];
        }

        return [.. weights!.Take(columnCount).Select(w => (double)w)];
    }
}
