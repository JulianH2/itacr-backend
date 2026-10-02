using Itacr.Api.Features.Catalog;
using Itacr.Api.Features.Catalog.Responses;

namespace Itacr.Api.Tests.Catalog;

/// <summary>In-memory catalog for API tests: the real products query needs SQL Server, which tests never open.</summary>
internal sealed class FakeCatalogService(
    IReadOnlyList<CategoryResponse> categories,
    IReadOnlyList<ProductResponse> products) : ICatalogService
{
    public Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(categories);

    public Task<IReadOnlyList<ProductResponse>> GetProductsAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductResponse>>(
            [.. products.Where(product => product.CategoryId == categoryId)]);
}
