using System.Text;

namespace FirearmStudio.Infrastructure.Services;

internal static class RegisterCellText
{
    private const int BreakInterval = 3;
    private const char BreakOpportunity = '\u200B';

    public static string Sanitise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static string InsertBreakOpportunities(string? value, double usableWidth, Func<string, double> measure)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var runStart = 0;

        for (var i = 0; i <= value.Length; i++)
        {
            var atRunBoundary = i == value.Length || char.IsWhiteSpace(value[i]);

            if (!atRunBoundary)
            {
                continue;
            }

            AppendRun(builder, value, runStart, i - runStart, usableWidth, measure);

            if (i < value.Length)
            {
                builder.Append(value[i]);
            }

            runStart = i + 1;
        }

        return builder.ToString();
    }

    private static void AppendRun(
        StringBuilder builder,
        string value,
        int start,
        int length,
        double usableWidth,
        Func<string, double> measure)
    {
        if (length == 0)
        {
            return;
        }

        var run = value.Substring(start, length);

        if (measure(run) <= usableWidth)
        {
            builder.Append(run);
            return;
        }

        var segmentStart = 0;

        for (var i = 0; i < run.Length; i++)
        {
            var isLastCharacterInRun = i == run.Length - 1;

            if (!IsSegmentBoundary(run[i]) && !isLastCharacterInRun)
            {
                continue;
            }

            AppendSegment(builder, run[segmentStart..(i + 1)], usableWidth, measure);

            if (!isLastCharacterInRun)
            {
                builder.Append(BreakOpportunity);
            }

            segmentStart = i + 1;
        }
    }

    private static void AppendSegment(
        StringBuilder builder,
        string segment,
        double usableWidth,
        Func<string, double> measure)
    {
        if (measure(segment) <= usableWidth)
        {
            builder.Append(segment);
            return;
        }

        for (var i = 0; i < segment.Length; i++)
        {
            builder.Append(segment[i]);

            var isLastCharacterInSegment = i == segment.Length - 1;
            var atBreakInterval = (i + 1) % BreakInterval == 0;

            if (atBreakInterval && !isLastCharacterInSegment)
            {
                builder.Append(BreakOpportunity);
            }
        }
    }

    private static bool IsSegmentBoundary(char value) => value is '-' or '/';
}
