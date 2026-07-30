using System.Text;

namespace FirearmStudio.Application.Common;

/// <summary>
/// Shared CSV row writing. Quoting follows RFC 4180: any field containing a comma, double quote,
/// carriage return, or line feed is wrapped in double quotes, and embedded double quotes are
/// doubled. Fields whose first character could be interpreted as a spreadsheet formula
/// (=, +, -, @, tab, or carriage return) are neutralized with a leading apostrophe before
/// RFC 4180 quoting is applied.
/// </summary>
public static class CsvWriting
{
    private static readonly char[] FormulaTriggerChars = ['=', '+', '-', '@', '\t', '\r'];

    public static void WriteRow(StringBuilder builder, string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(QuoteField(fields[i]));
        }

        builder.Append("\r\n");
    }

    private static string QuoteField(string value)
    {
        var neutralized = value.Length > 0 && Array.IndexOf(FormulaTriggerChars, value[0]) >= 0
            ? "'" + value
            : value;

        if (neutralized.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return neutralized;
        }

        return $"\"{neutralized.Replace("\"", "\"\"")}\"";
    }
}
