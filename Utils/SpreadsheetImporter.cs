using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InventoryManagementSystem.Utils;

public static class SpreadsheetImporter
{
    public static IReadOnlyList<IReadOnlyList<string>> ReadFirstSheet(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The selected Excel file does not exist.", filePath);
        }

        using var archive = OpenArchive(filePath);
        var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("The Excel file does not contain a readable first worksheet.");

        var sharedStrings = ReadSharedStrings(archive);
        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<IReadOnlyList<string>>();

        foreach (var row in worksheet.Descendants(ns + "row"))
        {
            var values = new Dictionary<int, string>();
            var maxColumn = 0;
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? string.Empty;
                var columnIndex = GetColumnIndex(reference);
                if (columnIndex <= 0)
                {
                    continue;
                }

                values[columnIndex] = ReadCellValue(ns, cell, sharedStrings);
                maxColumn = Math.Max(maxColumn, columnIndex);
            }

            var rowValues = new string[maxColumn];
            for (var column = 1; column <= maxColumn; column++)
            {
                rowValues[column - 1] = values.TryGetValue(column, out var value) ? value : string.Empty;
            }

            rows.Add(rowValues);
        }

        return rows;
    }

    private static ZipArchive OpenArchive(string filePath)
    {
        try
        {
            return ZipFile.OpenRead(filePath);
        }
        catch (InvalidDataException)
        {
            throw new InvalidOperationException("The selected file is not a valid .xlsx workbook. Download the template and save your entries as an Excel workbook (.xlsx), then try again.");
        }
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToList();
    }

    private static string ReadCellValue(XNamespace ns, XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value)).Trim();
        }

        var rawValue = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(rawValue, out var sharedStringIndex) &&
            sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex].Trim();
        }

        return rawValue.Trim();
    }

    private static int GetColumnIndex(string cellReference)
    {
        var match = Regex.Match(cellReference, "^[A-Z]+", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return 0;
        }

        var index = 0;
        foreach (var character in match.Value.ToUpperInvariant())
        {
            index = (index * 26) + character - 'A' + 1;
        }

        return index;
    }
}
