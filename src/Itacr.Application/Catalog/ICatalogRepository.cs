using Itacr.Domain.Catalog;

namespace Itacr.Application.Catalog;

public interface ICatalogRepository
{
    /// <summary>Every category ordered by <c>SortOrder</c>.</summary>
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>The products of a category ordered by name; empty for an unknown category.</summary>
    Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);
}
