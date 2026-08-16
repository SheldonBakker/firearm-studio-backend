using System.Text;

namespace FirearmStudio.Application.Common;

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
