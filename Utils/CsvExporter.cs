using System.Text;

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
