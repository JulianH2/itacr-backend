using Itacr.Application.Catalog.Responses;

namespace Itacr.Application.Catalog;

/// <summary>Read-only access to the restaurant menu.</summary>
public interface ICatalogService
{
    /// <summary>Returns every category ordered by <c>SortOrder</c>.</summary>
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Returns the products of a category ordered by name; an unknown or empty category returns an empty list.</summary>
    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(Guid categoryId, CancellationToken cancellationToken);
}
