using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class CategoryService(ICategoryRepository categoryRepository, AuthorizationService authorizationService, AuditLogService auditLogService)
{
    public Task<IReadOnlyList<Category>> GetAllAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return categoryRepository.GetAllAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(Session session, string name, string description, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        Validate(name, description);
        var categoryId = await categoryRepository.CreateAsync(name.Trim(), description.Trim(), cancellationToken);
        await auditLogService.LogAsync(session, "Create", "Category", categoryId, null, $"Created category: {name}");
        InventoryEvents.RaiseCategoryChanged();
        return categoryId;
    }

    public async Task UpdateAsync(Session session, Category category, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        ArgumentNullException.ThrowIfNull(category);
        Validate(category.Name, category.Description);
        await categoryRepository.UpdateAsync(category, cancellationToken);
        await auditLogService.LogAsync(session, "Update", "Category", category.CategoryId, null, $"Updated category: {category.Name}");
        InventoryEvents.RaiseCategoryChanged();
    }

    public async Task DeleteAsync(Session session, int categoryId, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId));
        }

        if (await categoryRepository.HasProductsAsync(categoryId, cancellationToken))
        {
            throw new InvalidOperationException("This category contains products. Move those products to another category before deleting it.");
        }

        await categoryRepository.DeleteAsync(categoryId, cancellationToken);
        await auditLogService.LogAsync(session, "Delete", "Category", categoryId, null, $"Deleted category ID: {categoryId}");
        InventoryEvents.RaiseCategoryChanged();
    }

    private static void Validate(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ArgumentException("Category name is required and must be no longer than 100 characters.");
        }

        if (description.Trim().Length > 500)
        {
            throw new ArgumentException("Category description must be no longer than 500 characters.");
        }
    }
}
