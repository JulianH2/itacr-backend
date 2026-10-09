using Itacr.Domain.Catalog;

namespace Itacr.Application.Catalog;

public interface ICatalogRepository
{
    /// <summary>Every category ordered by <c>SortOrder</c>.</summary>
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>The products of a category ordered by name; empty for an unknown category.</summary>
    Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>The products with these ids; ids that do not exist are left out.</summary>
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
