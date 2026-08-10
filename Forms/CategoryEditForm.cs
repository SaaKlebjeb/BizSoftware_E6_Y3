using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class CategoryEditForm : Form
{
    private readonly CategoryService _categoryService;
    private readonly Session _session;
    private readonly Category? _existingCategory;
    private readonly TextBox _nameTextBox = new() { Name = "categoryNameTextBox" };
    private readonly TextBox _descriptionTextBox = new() { Name = "categoryDescriptionTextBox", Multiline = true, Height = 90, ScrollBars = ScrollBars.Vertical };
    private readonly Label _errorLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public CategoryEditForm(CategoryService categoryService, Session session, Category? existingCategory = null)
    {
        _categoryService = categoryService;
        _session = session;
        _existingCategory = existingCategory;
        Text = existingCategory is null ? "Add Category" : "Edit Category";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 300);
        ClientSize = new Size(560, 330);
        BuildUi();
    }

    private void BuildUi()
    {
        if (_existingCategory is not null)
        {
            _nameTextBox.Text = _existingCategory.Name;
            _descriptionTextBox.Text = _existingCategory.Description;
        }

        var saveButton = new Button { Text = "Save", AutoSize = true };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        saveButton.Click += async (_, _) => await SaveAsync();
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(layout, 0, "Name", _nameTextBox);
        AddField(layout, 1, "Description", _descriptionTextBox);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(24, 8, 24, 8) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        _errorLabel.Dock = DockStyle.Bottom;
        _errorLabel.Height = 38;
        _errorLabel.Padding = new Padding(24, 6, 24, 0);
        Controls.Add(layout);
        Controls.Add(buttons);
        Controls.Add(_errorLabel);
    }

    private static void AddField(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, row);
        control.Dock = DockStyle.Top;
        layout.Controls.Add(control, 1, row);
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_existingCategory is null)
            {
                await _categoryService.CreateAsync(_session, _nameTextBox.Text, _descriptionTextBox.Text);
            }
            else
            {
                await _categoryService.UpdateAsync(_session, new Category
                {
                    CategoryId = _existingCategory.CategoryId,
                    Name = _nameTextBox.Text,
                    Description = _descriptionTextBox.Text,
                    CreatedAt = _existingCategory.CreatedAt
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            _errorLabel.Text = UserMessageFormatter.From(exception);
        }
    }
}
