using System.ComponentModel;
using System.Drawing.Printing;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class InvoicePreviewForm : Form
{
    private readonly SalesService _salesService;
    private readonly Session _session;
    private readonly Sale _sale;
    private readonly string _applicationName;
    private readonly DataGridView _itemsGrid = new() { Name = "invoiceItemsGrid" };
    private readonly Label _totalLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private readonly Button _confirmButton = new() { Text = "Confirm & record sale", AutoSize = true };
    private readonly Button _printButton = new() { Text = "Print invoice", AutoSize = true };
    private readonly PrintDocument _printDocument = new();
    private int _printIndex;

    public int? RecordedSaleId { get; private set; }

    public InvoicePreviewForm(SalesService salesService, Session session, Sale sale, string applicationName = "Inventory Management System")
    {
        _salesService = salesService;
        _session = session;
        _sale = sale;
        _applicationName = string.IsNullOrWhiteSpace(applicationName) ? "Inventory Management System" : applicationName.Trim();
        Text = "Invoice preview - confirm sale";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 500);
        ClientSize = new Size(920, 620);
        BuildUi();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 104, Padding = new Padding(24, 14, 24, 10) };
        header.Controls.Add(new Label { Text = _applicationName, AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top });
        header.Controls.Add(new Label { Text = "INVOICE PREVIEW", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), Dock = DockStyle.Top });
        header.Controls.Add(new Label { Text = $"Date: {DateTimeHelper.FormatForDisplay(_sale.SaleDate)}    |    Cashier: {_session.FullName}", AutoSize = true, Dock = DockStyle.Bottom });

        ConfigureGrid();
        var totalPanel = new Panel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(24, 8, 24, 8) };
        _totalLabel.Text = $"Total: {_sale.TotalAmount:N2}";
        _totalLabel.Dock = DockStyle.Right;
        totalPanel.Controls.Add(_totalLabel);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(24, 8, 24, 8) };
        var cancelButton = new Button { Text = "Back", AutoSize = true, DialogResult = DialogResult.Cancel };
        _confirmButton.BackColor = Color.FromArgb(30, 115, 190);
        _confirmButton.ForeColor = Color.White;
        _confirmButton.Click += async (_, _) => await ConfirmAsync();
        _printButton.Click += (_, _) => PrintInvoice();
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(_confirmButton);
        buttons.Controls.Add(_printButton);
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 34;
        _statusLabel.Padding = new Padding(24, 6, 24, 0);
        AcceptButton = _confirmButton;
        CancelButton = cancelButton;

        Controls.Add(_itemsGrid);
        Controls.Add(totalPanel);
        Controls.Add(buttons);
        Controls.Add(_statusLabel);
        Controls.Add(header);
    }

    private void ConfigureGrid()
    {
        _itemsGrid.Dock = DockStyle.Fill;
        _itemsGrid.ReadOnly = true;
        _itemsGrid.AllowUserToAddRows = false;
        _itemsGrid.AutoGenerateColumns = false;
        _itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _itemsGrid.MultiSelect = false;
        _itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _itemsGrid.BackgroundColor = Color.White;
        _itemsGrid.RowHeadersVisible = false;
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", DataPropertyName = nameof(InvoiceRow.Sku) });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product", DataPropertyName = nameof(InvoiceRow.Product) });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = nameof(InvoiceRow.Quantity) });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit price", DataPropertyName = nameof(InvoiceRow.UnitPrice), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", DataPropertyName = nameof(InvoiceRow.Subtotal), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _itemsGrid.DataSource = _sale.Items.Select(item => new InvoiceRow(item.ProductSku, item.ProductName, item.Quantity, item.UnitPrice, item.Subtotal)).ToList();
    }

    private async Task ConfirmAsync()
    {
        try
        {
            _confirmButton.Enabled = false;
            _printButton.Enabled = false;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = "Recording sale and updating stock...";
            RecordedSaleId = await _salesService.ConfirmSaleAsync(_session, _sale);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            _confirmButton.Enabled = true;
            _printButton.Enabled = true;
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private void PrintInvoice()
    {
        try
        {
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                MessageBox.Show(this, "No printer is installed. Install a printer or Microsoft Print to PDF to print the invoice.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printIndex = 0;
            _printDocument.DocumentName = "Invoice";
            _printDocument.PrintController = new PreviewPrintController();
            _printDocument.PrintPage -= PrintPage;
            _printDocument.PrintPage += PrintPage;
            using var preview = new PrintPreviewDialog { Document = _printDocument, Width = 1_000, Height = 700 };
            preview.ShowDialog(this);
        }
        catch (InvalidPrinterException)
        {
            MessageBox.Show(this, "No valid printer is available.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1722 || exception.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "The Windows Print Spooler service is unavailable.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private void PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (e.Graphics is null)
        {
            e.HasMorePages = false;
            return;
        }

        using var titleFont = new Font("Segoe UI", 17, FontStyle.Bold);
        using var systemFont = new Font("Segoe UI", 11, FontStyle.Bold);
        using var font = new Font("Segoe UI", 9);
        using var boldFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var grayBrush = new SolidBrush(Color.FromArgb(90, 90, 90));
        using var headerBrush = new SolidBrush(Color.FromArgb(232, 240, 248));
        using var linePen = new Pen(Color.FromArgb(170, 180, 190));
        using var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        using var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

        var graphics = e.Graphics;
        var left = (float)e.MarginBounds.Left;
        var right = (float)e.MarginBounds.Right;
        var tableWidth = right - left;
        var skuWidth = tableWidth * 0.18f;
        var productWidth = tableWidth * 0.40f;
        var quantityWidth = tableWidth * 0.10f;
        var unitPriceWidth = tableWidth * 0.16f;
        var subtotalWidth = tableWidth - skuWidth - productWidth - quantityWidth - unitPriceWidth;
        var y = (float)e.MarginBounds.Top;

        graphics.DrawString(_applicationName, systemFont, Brushes.Black, left, y);
        y += 25;
        graphics.DrawString("SALES INVOICE", titleFont, Brushes.Black, left, y);
        y += 34;
        graphics.DrawString($"Date: {DateTimeHelper.FormatForDisplay(_sale.SaleDate)}", font, grayBrush, left, y);
        graphics.DrawString($"Cashier: {_session.FullName}", font, grayBrush, left + tableWidth * 0.48f, y);
        y += 24;
        graphics.DrawLine(linePen, left, y, right, y);
        y += 10;

        var headerHeight = 27f;
        graphics.FillRectangle(headerBrush, left, y, tableWidth, headerHeight);

        var x = left;
        DrawCell(graphics, "SKU", boldFont, Brushes.Black, new RectangleF(x, y, skuWidth, headerHeight), leftFormat);
        x += skuWidth;
        DrawCell(graphics, "Product", boldFont, Brushes.Black, new RectangleF(x, y, productWidth, headerHeight), leftFormat);
        x += productWidth;
        DrawCell(graphics, "Qty", boldFont, Brushes.Black, new RectangleF(x, y, quantityWidth, headerHeight), rightFormat);
        x += quantityWidth;
        DrawCell(graphics, "Unit price", boldFont, Brushes.Black, new RectangleF(x, y, unitPriceWidth, headerHeight), rightFormat);
        x += unitPriceWidth;
        DrawCell(graphics, "Subtotal", boldFont, Brushes.Black, new RectangleF(x, y, subtotalWidth, headerHeight), rightFormat);
        y += headerHeight;

        while (_printIndex < _sale.Items.Count)
        {
            var item = _sale.Items[_printIndex++];
            var rowHeight = 25f;
            x = left;
            DrawCell(graphics, item.ProductSku, font, Brushes.Black, new RectangleF(x, y, skuWidth, rowHeight), leftFormat);
            x += skuWidth;
            DrawCell(graphics, item.ProductName, font, Brushes.Black, new RectangleF(x, y, productWidth, rowHeight), leftFormat);
            x += productWidth;
            DrawCell(graphics, item.Quantity.ToString("N0"), font, Brushes.Black, new RectangleF(x, y, quantityWidth, rowHeight), rightFormat);
            x += quantityWidth;
            DrawCell(graphics, item.UnitPrice.ToString("N2"), font, Brushes.Black, new RectangleF(x, y, unitPriceWidth, rowHeight), rightFormat);
            x += unitPriceWidth;
            DrawCell(graphics, item.Subtotal.ToString("N2"), font, Brushes.Black, new RectangleF(x, y, subtotalWidth, rowHeight), rightFormat);
            graphics.DrawLine(linePen, left, y + rowHeight, right, y + rowHeight);
            y += rowHeight;

            if (y + 75 > e.MarginBounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }
        }

        y += 22;
        graphics.DrawLine(linePen, left + tableWidth * 0.60f, y, right, y);
        y += 12;
        graphics.DrawString("TOTAL", boldFont, Brushes.Black, left + tableWidth * 0.60f, y);
        graphics.DrawString(_sale.TotalAmount.ToString("N2"), titleFont, Brushes.Black, new RectangleF(right - tableWidth * 0.35f, y - 4, tableWidth * 0.35f, 32), rightFormat);
        e.HasMorePages = false;
    }

    private static void DrawCell(Graphics graphics, string value, Font font, Brush brush, RectangleF bounds, StringFormat format) =>
        graphics.DrawString(value, font, brush, bounds, format);
}

public sealed record InvoiceRow(string Sku, string Product, int Quantity, decimal UnitPrice, decimal Subtotal);
