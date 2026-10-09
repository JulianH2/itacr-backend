using Itacr.Application.Catalog;
using Itacr.Domain.Catalog;

namespace Itacr.Api.Tests.Catalog;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ShouldUseAnEmptyDescription_WhenTheProductHasNone()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "Limonada", Description = null, Price = 32m };
        var service = new CatalogService(new StubCatalogRepository([], [product]));

        // Act
        var products = await service.GetProductsAsync(categoryId, CancellationToken.None);

        // Assert
        var response = Assert.Single(products);
        Assert.Equal(string.Empty, response.Description);
        Assert.Equal(32m, response.Price);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldKeepTheRepositoryOrder_WhenItReturnsCategories()
    {
        // Arrange
        Category[] categories = [new() { Id = Guid.NewGuid(), Name = "Combos", SortOrder = 1 }, new() { Id = Guid.NewGuid(), Name = "Bebidas", SortOrder = 4 }];
        var service = new CatalogService(new StubCatalogRepository(categories, []));

        // Act
        var result = await service.GetCategoriesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["Combos", "Bebidas"], result.Select(category => category.Name));
        Assert.Equal([1, 4], result.Select(category => category.SortOrder));
    }

    private sealed class StubCatalogRepository(IReadOnlyList<Category> categories, IReadOnlyList<Product> products) : ICatalogRepository
    {
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken) => Task.FromResult(categories);

        public Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>([.. products.Where(product => product.CategoryId == categoryId)]);
    }
}
