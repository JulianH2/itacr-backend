using Itacr.Application.Catalog;
using Itacr.Application.Catalog.GetCategories;
using Itacr.Application.Catalog.GetProducts;
using Itacr.Domain.Catalog;

namespace Itacr.Api.Tests.Catalog;

public sealed class CatalogHandlersTests
{
    [Fact]
    public async Task GetProducts_ShouldUseAnEmptyDescription_WhenTheProductHasNone()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "Limonada", Description = null, Price = 32m };
        var handler = new GetProductsHandler(new StubCatalogRepository([], [product]), new GetProductsValidator());

        // Act
        var result = await handler.HandleAsync(new GetProductsQuery(categoryId), CancellationToken.None);

        // Assert
        var response = Assert.Single(result.Value);
        Assert.Equal(string.Empty, response.Description);
        Assert.Equal(32m, response.Price);
    }

    [Fact]
    public async Task GetCategories_ShouldKeepTheRepositoryOrder_WhenItReturnsCategories()
    {
        // Arrange
        Category[] categories = [new() { Id = Guid.NewGuid(), Name = "Combos", SortOrder = 1 }, new() { Id = Guid.NewGuid(), Name = "Bebidas", SortOrder = 4 }];
        var handler = new GetCategoriesHandler(new StubCatalogRepository(categories, []));

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["Combos", "Bebidas"], result.Value.Select(category => category.Name));
        Assert.Equal([1, 4], result.Value.Select(category => category.SortOrder));
    }

    private sealed class StubCatalogRepository(IReadOnlyList<Category> categories, IReadOnlyList<Product> products) : ICatalogRepository
    {
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken) => Task.FromResult(categories);

        public Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>([.. products.Where(product => product.CategoryId == categoryId)]);

        public Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>([.. products.Where(product => ids.Contains(product.Id))]);
    }
}
