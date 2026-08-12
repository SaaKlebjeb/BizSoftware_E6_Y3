using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using System.ComponentModel;

namespace InventoryManagementSystem.Forms;

public sealed class ImportPreviewForm : Form
{
    private readonly ProductService _productService;
    private readonly Session _session;
    private readonly string _filePath;
    private readonly DataGridView _grid = new() { Name = "previewGrid" };
    private readonly Label _summaryLabel = new() { AutoSize = true, Padding = new Padding(8) };
    private readonly Button _importButton = new() { Text = "Import", AutoSize = true, Enabled = false };
    private readonly Button _cancelButton = new() { Text = "Cancel", AutoSize = true };
    private ImportPreviewResult? _previewResult;

    public ImportPreviewForm(ProductService productService, Session session, string filePath)
    {
        _productService = productService;
        _session = session;
        _filePath = filePath;
        Text = "Import Preview";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 500);
        ClientSize = new Size(1000, 600);
        BuildUi();
        Load += async (_, _) => await LoadPreviewAsync();
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, Padding = new Padding(8) };
        var refreshButton = new Button { Text = "Refresh Preview", AutoSize = true };
        toolbar.Controls.Add(refreshButton);
        toolbar.Controls.Add(_summaryLabel);
        refreshButton.Click += async (_, _) => await LoadPreviewAsync();

        ConfigureGrid();

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        _importButton.Click += async (_, _) => await ImportAsync();
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_importButton);

        Controls.Add(_grid);
        Controls.Add(buttonPanel);
        Controls.Add(toolbar);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = Color.FromArgb(210, 218, 226);
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 30;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 34;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(30, 115, 190),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(25, 25, 25),
            SelectionBackColor = Color.FromArgb(44, 125, 194),
            SelectionForeColor = Color.White,
            Padding = new Padding(5, 0, 5, 0)
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 247, 250)
        };

        AddColumn("Row", nameof(ImportPreviewRow.RowNumber), 60);
        AddColumn("SKU", nameof(ImportPreviewRow.Sku), 100);
        AddColumn("Product Name", nameof(ImportPreviewRow.Name), 200);
        AddColumn("Category", nameof(ImportPreviewRow.Category), 120);
        AddColumn("Price", nameof(ImportPreviewRow.Price), 80, "N2");
        AddColumn("Qty", nameof(ImportPreviewRow.Quantity), 60);
        AddColumn("Threshold", nameof(ImportPreviewRow.LowStockThreshold), 80);
        AddColumn("Status", nameof(ImportPreviewRow.IsValid), 80);
        AddColumn("Error", nameof(ImportPreviewRow.ErrorMessage), 250);

        _grid.CellFormatting += Grid_CellFormatting;
    }

    private void AddColumn(string header, string property, int width = 100, string? format = null)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            SortMode = DataGridViewColumnSortMode.Automatic,
            Width = width,
            DefaultCellStyle = format is null ? null : new DataGridViewCellStyle { Format = format }
        });
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].DataPropertyName == nameof(ImportPreviewRow.IsValid) && e.Value is bool isValid)
        {
            e.Value = isValid ? "✓ Valid" : "✗ Invalid";
            e.FormattingApplied = true;
            if (e.RowIndex >= 0 && e.RowIndex < _grid.Rows.Count)
            {
                var row = _grid.Rows[e.RowIndex];
                row.DefaultCellStyle.BackColor = isValid ? Color.White : Color.MistyRose;
                row.DefaultCellStyle.ForeColor = isValid ? Color.FromArgb(25, 25, 25) : Color.DarkRed;
            }
        }
    }

    private async Task LoadPreviewAsync()
    {
        _grid.DataSource = null;
        _importButton.Enabled = false;
        _summaryLabel.Text = "Loading preview...";

        try
        {
            _previewResult = await _productService.PreviewImportAsync(_session, _filePath);
            if (_previewResult is null)
            {
                MessageBox.Show(this, "Failed to load preview.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_previewResult.Errors.Count > 0)
            {
                _grid.DataSource = new List<ImportPreviewRow>();
                _summaryLabel.Text = $"Preview failed: {string.Join("; ", _previewResult.Errors)}";
                return;
            }

            _grid.DataSource = _previewResult.Rows.ToList();
            var validCount = _previewResult.Rows.Count(r => r.IsValid);
            var invalidCount = _previewResult.Rows.Count - validCount;
            _summaryLabel.Text = $"Total rows: {_previewResult.Rows.Count} | Valid: {validCount} | Invalid: {invalidCount}";
            _importButton.Enabled = validCount > 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"Preview failed: {exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ImportAsync()
    {
        if (_previewResult is null || _previewResult.Rows.Count == 0)
        {
            return;
        }

        _importButton.Enabled = false;
        _cancelButton.Enabled = false;
        _summaryLabel.Text = "Importing...";

        try
        {
            var result = await _productService.ImportFromExcelAsync(_session, _filePath);
            if (result.Errors.Count > 0)
            {
                var errors = string.Join(Environment.NewLine, result.Errors.Take(10));
                MessageBox.Show(this, errors, "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _summaryLabel.Text = "Import failed";
            }
            else
            {
                DialogResult = DialogResult.OK;
                MessageBox.Show(this, $"{result.ImportedCount:N0} product(s) imported successfully.", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"Import failed: {exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _importButton.Enabled = _previewResult?.Rows.Any(r => r.IsValid) == true;
            _cancelButton.Enabled = true;
        }
    }
}