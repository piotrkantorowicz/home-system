namespace Budget.Application.StatementImports;

using System.Text;

/// <summary>
/// Splits one CSV line into fields: quoted fields may hold the delimiter, <c>""</c> is a literal quote.
/// As in RFC 4180, a quote opens a quoted field only at its start; mid-field it is plain text.
/// </summary>
internal static class CsvLine
{
    // ponytail: one physical line is one record; no bank in bank-import.md §2.1 puts newlines inside a quoted field.
    public static string[] Split(string line, char delimiter)
    {
        List<string> fields = [];
        StringBuilder field = new();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c != '"')
                    field.Append(c);
                else if (i + 1 < line.Length && line[i + 1] == '"')
                    field.Append(line[++i]);
                else
                    quoted = false;
            }
            else if (c == '"' && field.Length == 0)
                quoted = true;
            else if (c == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
                field.Append(c);
        }

        fields.Add(field.ToString());
        return [.. fields];
    }
}
