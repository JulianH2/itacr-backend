using Itacr.Application.Catalog;
using Itacr.Application.Catalog.GetCategories;
using Itacr.Application.Catalog.GetProducts;
using Itacr.Domain.Catalog;

namespace Itacr.Api.Tests.Catalog;

/// <summary>In-memory catalog for API tests that do not need SQL Server, built from the responses the tests expect.</summary>
internal sealed class FakeCatalogRepository(
    IReadOnlyList<CategoryResponse> categories,
    IReadOnlyList<ProductResponse> products) : ICatalogRepository
{
    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Category>>(
            [.. categories.Select(category => new Category { Id = category.Id, Name = category.Name, SortOrder = category.SortOrder })]);

    public Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>([.. products.Where(product => product.CategoryId == categoryId).Select(ToProduct)]);

    public Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>([.. products.Where(product => ids.Contains(product.Id)).Select(ToProduct)]);

    private static Product ToProduct(ProductResponse product) => new()
    {
        Id = product.Id,
        CategoryId = product.CategoryId,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        IsCombo = product.IsCombo,
    };
}
