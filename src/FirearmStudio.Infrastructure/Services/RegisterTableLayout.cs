namespace FirearmStudio.Infrastructure.Services;

/// <summary>
/// Turns the register's relative column weights into the absolute column widths MigraDoc needs.
/// MigraDoc does not scale a table to fit: widths that overshoot the content box push columns
/// silently off the page, so the result is guaranteed to sum to exactly the content width.
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

        var effective = EffectiveWeights(columnCount, weights);
        var total = effective.Sum();

        var widths = new double[columnCount];
        var assigned = 0d;

        // The last column takes the remainder so rounding drift can never overflow the page.
        for (var i = 0; i < columnCount - 1; i++)
        {
            widths[i] = Math.Max(MinimumWidth, contentWidth * effective[i] / total);
            assigned += widths[i];
        }

        widths[columnCount - 1] = Math.Max(MinimumWidth, contentWidth - assigned);
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
