using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;
using System.ComponentModel;
using System.Drawing.Printing;

namespace InventoryManagementSystem.Forms;

public sealed class AuditLogsForm : Form
{
    private readonly AuditLogService _auditLogService;
    private readonly Session _session;
    private readonly string? _entityName;
    private readonly int? _entityId;
    private readonly DateTimePicker _startDate = new() { Format = DateTimePickerFormat.Short, Name = "auditStartDate" };
    private readonly DateTimePicker _endDate = new() { Format = DateTimePickerFormat.Short, Name = "auditEndDate" };
    private readonly DataGridView _grid = new() { Name = "auditLogsGrid" };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private readonly PrintDocument _printDocument = new();
    private IReadOnlyList<AuditLog> _loadedLogs = [];
    private int _printIndex;

    public AuditLogsForm(AuditLogService auditLogService, Session session, string? entityName = null, int? entityId = null)
    {
        _auditLogService = auditLogService;
        _session = session;
        _entityName = entityName;
        _entityId = entityId;
        Text = string.IsNullOrWhiteSpace(entityName) ? "Audit Logs" : $"History - {entityName} #{entityId}";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        _startDate.Value = DateTime.Today.AddMonths(-12);
        _endDate.Value = DateTime.Today;
        BuildUi();
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        toolbar.Controls.Add(new Label { Text = "From", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        toolbar.Controls.Add(_startDate);
        toolbar.Controls.Add(new Label { Text = "To", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
        toolbar.Controls.Add(_endDate);
        var filterButton = new Button { Text = "Filter", AutoSize = true };
        var exportButton = new Button { Text = "Export Excel", AutoSize = true };
        var printButton = new Button { Text = "Print preview", AutoSize = true };
        var refreshButton = new Button { Text = "Refresh", AutoSize = true };
        filterButton.Click += async (_, _) => await LoadAsync();
        exportButton.Click += (_, _) => ExportExcel();
        printButton.Click += (_, _) => ShowPrintPreview();
        refreshButton.Click += async (_, _) => await LoadAsync();
        toolbar.Controls.Add(filterButton);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(printButton);
        toolbar.Controls.Add(refreshButton);
        toolbar.Controls.Add(_statusLabel);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 30;
        AddColumn("Date", nameof(AuditLog.CreatedAt), "g");
        AddColumn("User", nameof(AuditLog.Username));
        AddColumn("Action", nameof(AuditLog.Action));
        AddColumn("Entity", nameof(AuditLog.EntityName));
        AddColumn("Entity ID", nameof(AuditLog.EntityId));
        AddColumn("Description", nameof(AuditLog.Description));

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private void AddColumn(string header, string property, string? format = null)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = format is null ? null : new DataGridViewCellStyle { Format = format }
        });
    }

    private async Task LoadAsync()
    {
        try
        {
            var startDate = ToUtcStart(_startDate.Value);
            var endDate = ToUtcEnd(_endDate.Value);
            if (endDate <= startDate)
            {
                throw new ArgumentException("The end date must be on or after the start date.");
            }

            var logs = await _auditLogService.GetRecentAsync(_session, startDate, endDate, _entityName, _entityId);
            _loadedLogs = logs;
            _grid.DataSource = _loadedLogs.ToList();
            _statusLabel.ForeColor = Color.DarkSlateGray;
            _statusLabel.Text = $"{logs.Count:N0} record(s)";
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private void ExportExcel()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = "audit-logs.xlsx"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SpreadsheetExporter.ExportXlsx(
            dialog.FileName,
            $"Audit Logs ({_startDate.Value:d} - {_endDate.Value:d})",
            new[] { "Date", "User", "Action", "Entity", "Entity ID", "Description" },
            _loadedLogs.Select(log => new object?[]
            {
                DateTimeHelper.FormatForDisplay(log.CreatedAt),
                log.Username,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.Description
            }));
        MessageBox.Show(this, "Audit logs exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            _printIndex = 0;
            _printDocument.PrintController = new PreviewPrintController();
            _printDocument.PrintPage -= PrintDocumentOnPrintPage;
            _printDocument.PrintPage += PrintDocumentOnPrintPage;
            using var preview = new PrintPreviewDialog { Document = _printDocument, Width = 1_000, Height = 700 };
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

    private void ShowPrinterUnavailableMessage(string reason) =>
        MessageBox.Show(this, $"{reason}\n\nStart the Windows Print Spooler service or install Microsoft Print to PDF, then try again.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void PrintDocumentOnPrintPage(object? sender, PrintPageEventArgs e)
    {
        using var font = new Font("Segoe UI", 8);
        using var titleFont = new Font("Segoe UI", 13, FontStyle.Bold);
        var y = e.MarginBounds.Top;
        e.Graphics?.DrawString($"Audit Logs: {_startDate.Value:d} - {_endDate.Value:d}", titleFont, Brushes.Black, e.MarginBounds.Left, y);
        y += 30;
        e.Graphics?.DrawString("Date        User        Action        Entity        Description", font, Brushes.Black, e.MarginBounds.Left, y);
        y += 20;

        while (_printIndex < _loadedLogs.Count)
        {
            var log = _loadedLogs[_printIndex++];
            var line = $"{DateTimeHelper.FormatForDisplay(log.CreatedAt)}  {log.Username}  {log.Action}  {log.EntityName} #{log.EntityId}  {log.Description}";
            e.Graphics?.DrawString(line, font, Brushes.Black, e.MarginBounds.Left, y);
            y += 18;
            if (y + 18 > e.MarginBounds.Bottom)
            {
                e.HasMorePages = _printIndex < _loadedLogs.Count;
                return;
            }
        }

        e.HasMorePages = false;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadAsync();
    }

    private static DateTime ToUtcStart(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date, DateTimeKind.Local).ToUniversalTime();

    private static DateTime ToUtcEnd(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
}
