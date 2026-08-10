using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Windows.Forms.DataVisualization.Charting;

namespace InventoryManagementSystem.Forms;

public sealed class ReportsForm : Form
{
    private readonly ReportService _reportService;
    private readonly Session _session;
    private readonly DateTimePicker _startDate = new() { Name = "reportStartDate", Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _endDate = new() { Name = "reportEndDate", Format = DateTimePickerFormat.Short };
    private readonly DataGridView _dailyGrid = new() { Name = "dailySalesGrid" };
    private readonly DataGridView _topProductsGrid = new() { Name = "topProductsGrid" };
    private readonly Chart _dailyChart = CreateChart("Daily sales");
    private readonly Chart _topProductsChart = CreateChart("Top products");
    private readonly PrintDocument _printDocument = new();
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public ReportsForm(ReportService reportService, Session session)
    {
        _reportService = reportService;
        _session = session;
        Text = "Reports";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        _startDate.Value = DateTime.Today.AddDays(-30);
        BuildUi();
        Load += async (_, _) => await LoadReportsAsync();
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        var refresh = new Button { Text = "Run report", AutoSize = true };
        var export = new Button { Text = "Export CSV", AutoSize = true };
        var print = new Button { Text = "Print preview", AutoSize = true };
        toolbar.Controls.Add(new Label { Text = "From", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        toolbar.Controls.Add(_startDate);
        toolbar.Controls.Add(new Label { Text = "To", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        toolbar.Controls.Add(_endDate);
        toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(export);
        toolbar.Controls.Add(print);
        refresh.Click += async (_, _) => await LoadReportsAsync();
        export.Click += (_, _) => ExportDailySales();
        print.Click += (_, _) => ShowPrintPreview();

        ConfigureGrid(_dailyGrid, ("Date", nameof(DailySalesRow.Date)), ("Sales", nameof(DailySalesRow.NumberOfSales)), ("Total", nameof(DailySalesRow.TotalSales)));
        ConfigureGrid(_topProductsGrid, ("Product", nameof(TopProductRow.Product)), ("Quantity", nameof(TopProductRow.QuantitySold)), ("Revenue", nameof(TopProductRow.Revenue)));
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var dailyTab = new TabPage("Daily sales");
        _dailyChart.Dock = DockStyle.Bottom;
        _dailyChart.Height = 220;
        dailyTab.Controls.Add(_dailyGrid);
        dailyTab.Controls.Add(_dailyChart);
        var topTab = new TabPage("Top-selling products");
        _topProductsChart.Dock = DockStyle.Bottom;
        _topProductsChart.Height = 220;
        topTab.Controls.Add(_topProductsGrid);
        topTab.Controls.Add(_topProductsChart);
        tabs.TabPages.Add(dailyTab);
        tabs.TabPages.Add(topTab);
        _status.Dock = DockStyle.Bottom;
        _status.Height = 30;
        Controls.Add(tabs);
        Controls.Add(_status);
        Controls.Add(toolbar);
    }

    private static void ConfigureGrid(DataGridView grid, params (string Header, string Property)[] columns)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        foreach (var column in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = column.Header, DataPropertyName = column.Property, SortMode = DataGridViewColumnSortMode.Automatic });
        }
    }

    private async Task LoadReportsAsync()
    {
        try
        {
            var daily = await _reportService.GetDailySalesAsync(_session, _startDate.Value, _endDate.Value);
            var topProducts = await _reportService.GetTopProductsAsync(_session, _startDate.Value, _endDate.Value);
            _dailyGrid.DataSource = daily.ToList();
            _topProductsGrid.DataSource = topProducts.ToList();
            UpdateCharts(daily, topProducts);
            _status.ForeColor = Color.DarkGreen;
            _status.Text = $"Loaded {daily.Count} daily rows and {topProducts.Count} top products.";
        }
        catch (Exception exception)
        {
            _status.ForeColor = Color.Firebrick;
            _status.Text = UserMessageFormatter.From(exception);
        }
    }

    private void ExportDailySales()
    {
        if (_dailyGrid.DataSource is not IEnumerable<DailySalesRow> rows)
        {
            return;
        }

        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "daily-sales.csv" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            CsvExporter.Export(dialog.FileName, new[] { "Date", "Number of Sales", "Total Sales" }, rows.Select(row => new object?[] { row.Date, row.NumberOfSales, row.TotalSales }));
        }
    }

    private void UpdateCharts(IReadOnlyList<DailySalesRow> daily, IReadOnlyList<TopProductRow> topProducts)
    {
        var dailySeries = _dailyChart.Series[0];
        dailySeries.Points.Clear();
        foreach (var row in daily) dailySeries.Points.AddXY(row.Date.ToShortDateString(), row.TotalSales);

        var topSeries = _topProductsChart.Series[0];
        topSeries.Points.Clear();
        foreach (var row in topProducts.Take(10)) topSeries.Points.AddXY(row.Product, row.QuantitySold);
    }

    private void ShowPrintPreview()
    {
        try
        {
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                ShowPrinterUnavailableMessage("No printer is installed.");
                return;
            }

            _printDocument.PrintController = new PreviewPrintController();
            _printDocument.PrintPage -= PrintDocumentOnPrintPage;
            _printDocument.PrintPage += PrintDocumentOnPrintPage;
            using var preview = new PrintPreviewDialog { Document = _printDocument, Width = 900, Height = 700 };
            preview.ShowDialog(this);
        }
        catch (InvalidPrinterException)
        {
            ShowPrinterUnavailableMessage("No valid printer is available.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1722 || exception.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
        {
            ShowPrinterUnavailableMessage("The Windows Print Spooler service is unavailable.");
        }
    }

    private void ShowPrinterUnavailableMessage(string reason)
    {
        MessageBox.Show(this, $"{reason}\n\nStart the Windows Print Spooler service or install Microsoft Print to PDF, then try again. You can still export the report to CSV.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrintDocumentOnPrintPage(object? sender, PrintPageEventArgs e)
    {
        using var font = new Font("Segoe UI", 10);
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        var y = 40;
        e.Graphics?.DrawString($"Daily Sales Report: {_startDate.Value:d} - {_endDate.Value:d}", titleFont, Brushes.Black, 40, y);
        y += 36;
        if (_dailyGrid.DataSource is IEnumerable<DailySalesRow> rows)
        {
            foreach (var row in rows)
            {
                e.Graphics?.DrawString($"{row.Date:d}    {row.NumberOfSales} sales    {row.TotalSales:N2}", font, Brushes.Black, 40, y);
                y += 22;
                if (y > e.MarginBounds.Bottom) { e.HasMorePages = true; break; }
            }
        }
    }

    private static Chart CreateChart(string title)
    {
        var chart = new Chart { Name = title.Replace(" ", string.Empty, StringComparison.Ordinal) };
        chart.ChartAreas.Add(new ChartArea("Main"));
        chart.Titles.Add(title);
        chart.Legends.Clear();
        chart.Series.Add(new Series("Values") { ChartType = title.StartsWith("Daily", StringComparison.Ordinal) ? SeriesChartType.Line : SeriesChartType.Bar, IsValueShownAsLabel = true });
        return chart;
    }
}
