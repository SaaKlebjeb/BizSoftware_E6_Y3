using System.Text;
using System.Net;

namespace InventoryManagementSystem.Utils;

public static class CsvExporter
{
    public static void Export(string filePath, IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var builder = new StringBuilder();
        AppendRow(builder, headers);
        foreach (var row in rows)
        {
            AppendRow(builder, row);
        }

        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static void ExportHtmlTable(string filePath, string title, IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html><head><meta charset=\"utf-8\"><style>");
        builder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;color:#000;background:#fff}table{border-collapse:collapse;width:100%}th,td{border:1px solid #9aa7b2;padding:8px;text-align:left}th{background:#1e73be;color:#fff;font-weight:600}tr:nth-child(even){background:#f1f5f9}h1{font-size:20px}");
        builder.AppendLine("</style></head><body>");
        builder.Append("<h1>").Append(WebUtility.HtmlEncode(title)).AppendLine("</h1>");
        builder.AppendLine("<table><thead><tr>");
        foreach (var header in headers)
        {
            builder.Append("<th>").Append(WebUtility.HtmlEncode(header)).AppendLine("</th>");
        }

        builder.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            foreach (var value in row)
            {
                builder.Append("<td>").Append(WebUtility.HtmlEncode(value?.ToString() ?? string.Empty)).AppendLine("</td>");
            }

            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</tbody></table></body></html>");
        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<object?> values)
    {
        builder.AppendJoin(',', values.Select(value => Escape(value?.ToString() ?? string.Empty)));
        builder.AppendLine();
    }

    private static string Escape(string value) =>
        value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
