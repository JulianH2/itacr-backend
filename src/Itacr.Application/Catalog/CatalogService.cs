using Itacr.Application.Catalog.Responses;
using Itacr.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Application.Catalog;

public sealed class CatalogService(IItacrDbContext dbContext) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        // An inline function cannot ORDER BY, so the ordering is composed here on top of it.
        var rows = await dbContext.Database
            .SqlQuery<ProductRow>($"SELECT * FROM dbo.fn_GetProductsByCategory({categoryId})")
            .OrderBy(row => row.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(ToResponse);
    }

    private static ProductResponse ToResponse(ProductRow row) => new(
        row.Id,
        row.CategoryId,
        row.Name,
        row.Description ?? string.Empty,
        row.Price,
        row.ImageUrl,
        row.IsCombo);
}
