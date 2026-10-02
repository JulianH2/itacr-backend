using Itacr.Api.Features.Catalog;
using Itacr.Api.Tests.Persistence;

namespace Itacr.Api.Tests.Catalog;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnSeededCategoriesInDisplayOrder_WhenDatabaseIsSeeded()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(GetCategoriesAsync_ShouldReturnSeededCategoriesInDisplayOrder_WhenDatabaseIsSeeded));
        var service = new CatalogService(context);

        // Act
        var categories = await service.GetCategoriesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["Combos", "Hamburguesas", "Complementos", "Bebidas", "Postres"], categories.Select(category => category.Name));
        Assert.Equal([1, 2, 3, 4, 5], categories.Select(category => category.SortOrder));
    }
}
