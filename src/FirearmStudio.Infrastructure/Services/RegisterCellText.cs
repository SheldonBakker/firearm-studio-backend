using System.Text;

namespace FirearmStudio.Infrastructure.Services;

internal static class RegisterCellText
{
    private const int LongRunThreshold = 6;
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

    public static string InsertBreakOpportunities(string? value)
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

            AppendRun(builder, value, runStart, i - runStart);

            if (i < value.Length)
            {
                builder.Append(value[i]);
            }

            runStart = i + 1;
        }

        return builder.ToString();
    }

    private static void AppendRun(StringBuilder builder, string value, int start, int length)
    {
        if (length <= LongRunThreshold)
        {
            builder.Append(value, start, length);
            return;
        }

        for (var i = 0; i < length; i++)
        {
            builder.Append(value[start + i]);

            var isLastCharacterInRun = i == length - 1;
            var atBreakInterval = (i + 1) % BreakInterval == 0;

            if (atBreakInterval && !isLastCharacterInRun)
            {
                builder.Append(BreakOpportunity);
            }
        }
    }
}
