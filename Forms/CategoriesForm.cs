using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class CategoriesForm : Form
{
    private readonly CategoryService _categoryService;
    private readonly Session _session;
    private readonly DataGridView _grid = new() { Name = "categoriesGrid" };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public CategoriesForm(CategoryService categoryService, Session session)
    {
        _categoryService = categoryService;
        _session = session;
        Text = "Categories";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.CategoryChanged += OnCategoryChanged;
        FormClosed += (_, _) => InventoryEvents.CategoryChanged -= OnCategoryChanged;
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, Padding = new Padding(0, 0, 0, 8) };
        var addButton = new Button { Text = "Add Category", AutoSize = true };
        var editButton = new Button { Text = "Edit", AutoSize = true };
        var deleteButton = new Button { Text = "Delete", AutoSize = true };
        addButton.Click += async (_, _) => await AddAsync();
        editButton.Click += async (_, _) => await EditAsync();
        deleteButton.Click += async (_, _) => await DeleteAsync();
        toolbar.Controls.Add(addButton);
        toolbar.Controls.Add(editButton);
        toolbar.Controls.Add(deleteButton);
        _status.Dock = DockStyle.Bottom;
        _status.Height = 32;
        ConfigureGrid();
        Controls.Add(_grid);
        Controls.Add(_status);
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
        _grid.RowHeadersVisible = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(Category.Name) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description", DataPropertyName = nameof(Category.Description) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Created", DataPropertyName = nameof(Category.CreatedAt), DefaultCellStyle = new DataGridViewCellStyle { Format = "g" } });
        _grid.CellDoubleClick += async (_, args) => { if (args.RowIndex >= 0) await EditAsync(); };
    }

    private async Task LoadAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync(_session);
            _grid.DataSource = categories.ToList();
            _status.ForeColor = Color.DarkGreen;
            _status.Text = $"{categories.Count} categor{(categories.Count == 1 ? "y" : "ies")} available.";
        }
        catch (Exception exception)
        {
            _status.ForeColor = Color.Firebrick;
            _status.Text = UserMessageFormatter.From(exception);
        }
    }

    private async Task AddAsync()
    {
        using var dialog = new CategoryEditForm(_categoryService, _session);
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadAsync();
    }

    private async Task EditAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Category category) return;
        using var dialog = new CategoryEditForm(_categoryService, _session, category);
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Category category) return;
        if (MessageBox.Show(this, $"Delete category '{category.Name}'?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            await _categoryService.DeleteAsync(_session, category.CategoryId);
            await LoadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to delete category", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void OnCategoryChanged(object? sender, EventArgs e) => await LoadAsync();

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadAsync();
    }
}
