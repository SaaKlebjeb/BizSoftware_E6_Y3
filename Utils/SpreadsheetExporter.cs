using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace InventoryManagementSystem.Utils;

public static class SpreadsheetExporter
{
    public static void ExportXlsx(
        string filePath,
        string sheetName,
        IEnumerable<string> headers,
        IEnumerable<IEnumerable<object?>> rows,
        string? title = null,
        string? subtitle = null)
        => ExportWorkbook(filePath, [new SpreadsheetSheet(sheetName, headers, rows, title, subtitle)]);

    public static void ExportWorkbook(string filePath, IReadOnlyList<SpreadsheetSheet> sheets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (sheets.Count == 0)
        {
            throw new ArgumentException("At least one sheet is required.", nameof(sheets));
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml(sheets.Count));
        WriteEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
        WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml(sheets.Select(sheet => NormalizeSheetName(sheet.SheetName)).ToArray(), sheets.Select(sheet => sheet.IsHidden).ToArray()));
        WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml(sheets.Count));
        WriteEntry(archive, "xl/styles.xml", BuildStylesXml());
        for (var index = 0; index < sheets.Count; index++)
        {
            var sheet = sheets[index];
            var headerRow = sheet.Headers.Select(header => header ?? string.Empty).ToArray();
            var dataRows = sheet.Rows.Select(row => row.ToArray()).ToList();
            WriteEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", BuildWorksheetXml(headerRow, dataRows, sheet.Title, sheet.Subtitle, sheet.DataValidations));
        }
    }

    private static string BuildContentTypesXml(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="utf-8" standalone="yes"?>""");
        builder.AppendLine("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        builder.AppendLine("""  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />""");
        builder.AppendLine("""  <Default Extension="xml" ContentType="application/xml" />""");
        builder.AppendLine("""  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />""");
        builder.AppendLine("""  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml" />""");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.AppendLine($"""  <Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />""");
        }

        builder.AppendLine("</Types>");
        return builder.ToString();
    }

    private static string BuildRootRelationshipsXml() =>
        """
        <?xml version="1.0" encoding="utf-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
        </Relationships>
        """;

    private static string BuildWorkbookXml(IReadOnlyList<string> sheetNames, IReadOnlyList<bool> hiddenStates)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var workbook = new XElement(ns + "workbook",
            new XAttribute(XNamespace.Xmlns + "r", rel),
            new XElement(ns + "sheets",
                sheetNames.Select((sheetName, index) =>
                {
                    var sheet = new XElement(ns + "sheet",
                        new XAttribute("name", sheetName),
                        new XAttribute("sheetId", index + 1),
                        new XAttribute(rel + "id", $"rId{index + 1}"));

                    if (index < hiddenStates.Count && hiddenStates[index])
                    {
                        sheet.Add(new XAttribute("state", "hidden"));
                    }

                    return sheet;
                })));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), workbook).ToString(SaveOptions.DisableFormatting);
    }

    private static string BuildWorkbookRelationshipsXml(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="utf-8" standalone="yes"?>""");
        builder.AppendLine("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.AppendLine($"""  <Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml" />""");
        }

        builder.AppendLine($"""  <Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml" />""");
        builder.AppendLine("</Relationships>");
        return builder.ToString();
    }

    private static string BuildStylesXml() =>
        """
        <?xml version="1.0" encoding="utf-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="4">
            <font>
              <sz val="11" />
              <color theme="1" />
              <name val="Calibri" />
              <family val="2" />
            </font>
            <font>
              <b />
              <sz val="14" />
              <color rgb="FFFFFFFF" />
              <name val="Calibri" />
              <family val="2" />
            </font>
            <font>
              <i />
              <sz val="10" />
              <color rgb="FF5B6570" />
              <name val="Calibri" />
              <family val="2" />
            </font>
            <font>
              <b />
              <sz val="11" />
              <color rgb="FFFFFFFF" />
              <name val="Calibri" />
              <family val="2" />
            </font>
          </fonts>
          <fills count="4">
            <fill>
              <patternFill patternType="none" />
            </fill>
            <fill>
              <patternFill patternType="gray125" />
            </fill>
            <fill>
              <patternFill patternType="solid">
                <fgColor rgb="FF1E73BE" />
                <bgColor indexed="64" />
              </patternFill>
            </fill>
            <fill>
              <patternFill patternType="solid">
                <fgColor rgb="FF2A5D8F" />
                <bgColor indexed="64" />
              </patternFill>
            </fill>
          </fills>
          <borders count="2">
            <border>
              <left />
              <right />
              <top />
              <bottom />
              <diagonal />
            </border>
            <border>
              <left style="thin"><color rgb="FFD9E2EC" /></left>
              <right style="thin"><color rgb="FFD9E2EC" /></right>
              <top style="thin"><color rgb="FFD9E2EC" /></top>
              <bottom style="thin"><color rgb="FFD9E2EC" /></bottom>
              <diagonal />
            </border>
          </borders>
          <cellStyleXfs count="1">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" />
          </cellStyleXfs>
          <cellXfs count="8">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" />
            <xf numFmtId="0" fontId="1" fillId="3" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1">
              <alignment horizontal="center" vertical="center" />
            </xf>
            <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1">
              <alignment horizontal="left" vertical="center" />
            </xf>
            <xf numFmtId="0" fontId="3" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1">
              <alignment horizontal="center" vertical="center" />
            </xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1">
              <alignment horizontal="left" vertical="center" />
            </xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1">
              <alignment horizontal="left" vertical="center" wrapText="1" />
            </xf>
            <xf numFmtId="14" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyNumberFormat="1" applyAlignment="1">
              <alignment horizontal="center" vertical="center" />
            </xf>
            <xf numFmtId="3" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyNumberFormat="1" applyAlignment="1">
              <alignment horizontal="right" vertical="center" />
            </xf>
            <xf numFmtId="4" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyNumberFormat="1" applyAlignment="1">
              <alignment horizontal="right" vertical="center" />
            </xf>
          </cellXfs>
          <cellStyles count="1">
            <cellStyle name="Normal" xfId="0" builtinId="0" />
          </cellStyles>
          <dxfs count="0" />
          <tableStyles count="0" defaultTableStyle="TableStyleMedium2" defaultPivotStyle="PivotStyleLight16" />
        </styleSheet>
        """;

    private static string BuildWorksheetXml(IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows, string? title, string? subtitle, IReadOnlyList<SpreadsheetDataValidation>? dataValidations)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheetData = new XElement(ns + "sheetData");
        var mergeCells = new List<string>();
        var rowNumber = 1;
        var columnCount = headers.Count;
        var lastColumn = GetColumnName(columnCount);
        if (!string.IsNullOrWhiteSpace(title))
        {
            sheetData.Add(BuildMergedTextRow(ns, rowNumber, title!, 1, columnCount, 1));
            mergeCells.Add($"A{rowNumber}:{lastColumn}{rowNumber}");
            rowNumber++;
        }

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            sheetData.Add(BuildMergedTextRow(ns, rowNumber, subtitle!, 2, columnCount, 2));
            mergeCells.Add($"A{rowNumber}:{lastColumn}{rowNumber}");
            rowNumber++;
        }

        var headerRowNumber = rowNumber;
        sheetData.Add(BuildHeaderRow(ns, headerRowNumber, headers));
        rowNumber++;

        for (var index = 0; index < rows.Count; index++, rowNumber++)
        {
            sheetData.Add(BuildDataRow(ns, rowNumber, headers, rows[index]));
        }

        var columnWidths = CalculateColumnWidths(headers, rows);
        var lastDataRow = rows.Count + headerRowNumber;
        var autoFilterRef = $"A{headerRowNumber}:{lastColumn}{lastDataRow}";

        var worksheet = new XElement(ns + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"),
            new XElement(ns + "sheetFormatPr", new XAttribute("defaultRowHeight", 20)),
            new XElement(ns + "cols",
                columnWidths.Select((width, index) => new XElement(ns + "col",
                    new XAttribute("min", index + 1),
                    new XAttribute("max", index + 1),
                    new XAttribute("width", width.ToString("0.00", CultureInfo.InvariantCulture)),
                    new XAttribute("customWidth", "1")))),
            sheetData,
            new XElement(ns + "autoFilter", new XAttribute("ref", autoFilterRef)),
            mergeCells.Count == 0 ? null : new XElement(ns + "mergeCells", new XAttribute("count", mergeCells.Count), mergeCells.Select(cell => new XElement(ns + "mergeCell", new XAttribute("ref", cell)))),
            dataValidations is null || dataValidations.Count == 0 ? null : new XElement(ns + "dataValidations",
                new XAttribute("count", dataValidations.Count),
                dataValidations.Select(validation => new XElement(ns + "dataValidation",
                    new XAttribute("type", "list"),
                    new XAttribute("allowBlank", validation.AllowBlank ? "1" : "0"),
                    new XAttribute("showErrorMessage", "1"),
                    new XAttribute("showInputMessage", "1"),
                    new XAttribute("sqref", validation.Sqref),
                    new XElement(ns + "formula1", validation.Formula1)))));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), worksheet).ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildMergedTextRow(XNamespace ns, int rowNumber, string text, int styleId, int columnCount, double height)
    {
        var row = new XElement(ns + "row", new XAttribute("r", rowNumber), new XAttribute("ht", height.ToString("0.##", CultureInfo.InvariantCulture)), new XAttribute("customHeight", "1"));
        row.Add(new XElement(ns + "c",
            new XAttribute("r", $"A{rowNumber}"),
            new XAttribute("s", styleId),
            new XAttribute("t", "inlineStr"),
            new XElement(ns + "is", new XElement(ns + "t", text))));
        return row;
    }

    private static XElement BuildHeaderRow(XNamespace ns, int rowNumber, IReadOnlyList<string> headers)
    {
        var row = new XElement(ns + "row", new XAttribute("r", rowNumber), new XAttribute("ht", 22), new XAttribute("customHeight", "1"));
        for (var i = 0; i < headers.Count; i++)
        {
            var cellReference = $"{GetColumnName(i + 1)}{rowNumber}";
            row.Add(new XElement(ns + "c",
                new XAttribute("r", cellReference),
                new XAttribute("s", 3),
                new XAttribute("t", "inlineStr"),
                new XElement(ns + "is", new XElement(ns + "t", headers[i]))));
        }

        return row;
    }

    private static XElement BuildDataRow(XNamespace ns, int rowNumber, IReadOnlyList<string> headers, IReadOnlyList<object?> values)
    {
        var row = new XElement(ns + "row", new XAttribute("r", rowNumber));
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var value = i < values.Count ? values[i] : null;
            row.Add(BuildDataCell(ns, $"{GetColumnName(i + 1)}{rowNumber}", header, value));
        }

        return row;
    }

    private static XElement BuildDataCell(XNamespace ns, string reference, string header, object? value)
    {
        return value switch
        {
            null => CreateInlineStringCell(ns, reference, string.Empty, 4),
            string text => CreateInlineStringCell(ns, reference, text, ShouldWrap(header, text) ? 5 : 4),
            DateTime dateTime => CreateNumericCell(ns, reference, dateTime.ToOADate().ToString(CultureInfo.InvariantCulture), 6),
            DateTimeOffset dateTimeOffset => CreateNumericCell(ns, reference, dateTimeOffset.DateTime.ToOADate().ToString(CultureInfo.InvariantCulture), 6),
            bool boolean => CreateInlineStringCell(ns, reference, boolean ? "Yes" : "No", 4),
            byte or sbyte or short or ushort or int or uint or long or ulong => CreateNumericCell(ns, reference, Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), 7),
            decimal decimalValue => CreateNumericCell(ns, reference, decimalValue.ToString(CultureInfo.InvariantCulture), 8),
            float floatValue => CreateNumericCell(ns, reference, floatValue.ToString(CultureInfo.InvariantCulture), 8),
            double doubleValue => CreateNumericCell(ns, reference, doubleValue.ToString(CultureInfo.InvariantCulture), 8),
            IFormattable formattable when IsLikelyNumericHeader(header) => CreateNumericCell(ns, reference, formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty, 8),
            _ => CreateInlineStringCell(ns, reference, value.ToString() ?? string.Empty, ShouldWrap(header, value.ToString() ?? string.Empty) ? 5 : 4)
        };
    }

    private static XElement CreateInlineStringCell(XNamespace ns, string reference, string value, int styleId) =>
        new(ns + "c",
            new XAttribute("r", reference),
            new XAttribute("s", styleId),
            new XAttribute("t", "inlineStr"),
            new XElement(ns + "is", new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value)));

    private static XElement CreateNumericCell(XNamespace ns, string reference, string value, int styleId) =>
        new(ns + "c",
            new XAttribute("r", reference),
            new XAttribute("s", styleId),
            new XElement(ns + "v", value));

    private static bool ShouldWrap(string header, string value) =>
        header.Contains("description", StringComparison.OrdinalIgnoreCase) ||
        header.Contains("note", StringComparison.OrdinalIgnoreCase) ||
        value.Length > 48;

    private static bool IsLikelyNumericHeader(string header) =>
        header.Contains("price", StringComparison.OrdinalIgnoreCase) ||
        header.Contains("total", StringComparison.OrdinalIgnoreCase) ||
        header.Contains("revenue", StringComparison.OrdinalIgnoreCase) ||
        header.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
        header.Contains("subtotal", StringComparison.OrdinalIgnoreCase);

    private static double[] CalculateColumnWidths(IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows)
    {
        var widths = new double[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            widths[i] = Math.Max(12, Math.Min(45, headers[i].Length + 4));
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                var text = columnIndex < row.Length ? FormatDisplayValue(row[columnIndex], headers[columnIndex]) : string.Empty;
                var estimatedWidth = Math.Max(10, Math.Min(60, text.Length + 3));
                widths[columnIndex] = Math.Max(widths[columnIndex], estimatedWidth);
            }
        }

        return widths;
    }

    private static string FormatDisplayValue(object? value, string header) =>
        value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture),
            decimal decimalValue => IsLikelyNumericHeader(header) ? decimalValue.ToString("N2", CultureInfo.CurrentCulture) : decimalValue.ToString(CultureInfo.CurrentCulture),
            float floatValue => IsLikelyNumericHeader(header) ? floatValue.ToString("N2", CultureInfo.CurrentCulture) : floatValue.ToString(CultureInfo.CurrentCulture),
            double doubleValue => IsLikelyNumericHeader(header) ? doubleValue.ToString("N2", CultureInfo.CurrentCulture) : doubleValue.ToString(CultureInfo.CurrentCulture),
            IFormattable formattable when IsIntegerValue(value) => formattable.ToString("N0", CultureInfo.CurrentCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };

    private static bool IsIntegerValue(object? value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong;

    private static string GetColumnName(int columnIndex)
    {
        var columnName = string.Empty;
        while (columnIndex > 0)
        {
            columnIndex--;
            columnName = (char)('A' + (columnIndex % 26)) + columnName;
            columnIndex /= 26;
        }

        return columnName;
    }

    private static string NormalizeSheetName(string sheetName)
    {
        var invalidChars = new[] { '[', ']', ':', '*', '?', '/', '\\' };
        var cleaned = new string(sheetName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return cleaned.Length == 0 ? "Sheet1" : cleaned[..Math.Min(cleaned.Length, 31)];
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    public sealed record SpreadsheetSheet(
        string SheetName,
        IEnumerable<string> Headers,
        IEnumerable<IEnumerable<object?>> Rows,
        string? Title = null,
        string? Subtitle = null,
        IReadOnlyList<SpreadsheetDataValidation>? DataValidations = null,
        bool IsHidden = false);

    public sealed record SpreadsheetDataValidation(string Sqref, string Formula1, bool AllowBlank = true);
}
