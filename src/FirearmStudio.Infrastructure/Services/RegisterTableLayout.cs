namespace FirearmStudio.Infrastructure.Services;

internal static class RegisterTableLayout
{
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
            for (var i = 0; i < columnCount - 1; i++)
            {
                widths[i] = contentWidth / columnCount;
                assigned += widths[i];
            }

            widths[columnCount - 1] = contentWidth - assigned;
            return widths;
        }

        var effective = EffectiveWeights(columnCount, weights);
        var total = effective.Sum();
        var distributable = contentWidth - floorBudget;

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
