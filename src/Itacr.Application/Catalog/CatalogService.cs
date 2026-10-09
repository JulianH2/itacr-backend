using Itacr.Application.Catalog.Responses;
using Itacr.Domain.Catalog;

namespace Itacr.Application.Catalog;

public sealed class CatalogService(ICatalogRepository catalogRepository) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await catalogRepository.GetCategoriesAsync(cancellationToken);
        return [.. categories.Select(category => new CategoryResponse(category.Id, category.Name, category.SortOrder))];
    }

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var products = await catalogRepository.GetProductsByCategoryAsync(categoryId, cancellationToken);
        return [.. products.Select(ToResponse)];
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.CategoryId,
        product.Name,
        product.Description ?? string.Empty,
        product.Price,
        product.ImageUrl,
        product.IsCombo);
}
