using System.Text;

namespace FirearmStudio.Infrastructure.Services;

/// <summary>
/// Flattens a register cell to a single line. MigraDoc parses tabs and newlines inside paragraph
/// text into layout nodes, which distorts row heights in the dense register table, so free-text
/// fields are collapsed to single spaces before they reach the document.
/// </summary>
internal static class RegisterCellText
{
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
}
