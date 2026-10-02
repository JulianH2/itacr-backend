using Itacr.Api.Features.Catalog;

namespace Itacr.Api.Tests.Persistence;

public sealed class CatalogSeedDataTests
{
    private const int MinProductsPerCategory = 3;
    private const int MaxProductsPerCategory = 4;

    [Fact]
    public void Catalog_ShouldContainTheExpectedReferenceData_WhenDatabaseIsCreated()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Catalog_ShouldContainTheExpectedReferenceData_WhenDatabaseIsCreated));

        // Act
        var categories = context.Categories.Select(category => category.Name).ToList();
        var tables = context.DiningTables.Select(table => table.Number).OrderBy(number => number).ToList();
        var options = context.ComboOptions.ToList();

        // Assert
        Assert.Equivalent(new[] { "Combos", "Hamburguesas", "Complementos", "Bebidas", "Postres" }, categories);
        Assert.Equal(Enumerable.Range(1, 10), tables);
        Assert.Equal(3, options.Count(option => option.Type == ComboOptionType.Size));
        Assert.Equal(3, options.Count(option => option.Type == ComboOptionType.Drink));
        Assert.Equal(3, options.Count(option => option.Type == ComboOptionType.Extra));
        Assert.Equal(25m, options.Single(option => option.Name == "Grande").PriceAdjustment);
    }

    [Fact]
    public void Catalog_ShouldHaveThreeOrFourProductsPerCategory_WhenDatabaseIsCreated()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Catalog_ShouldHaveThreeOrFourProductsPerCategory_WhenDatabaseIsCreated));

        // Act
        var counts = context.Products.GroupBy(product => product.CategoryId).Select(group => group.Count()).ToList();

        // Assert
        Assert.Equal(5, counts.Count);
        Assert.All(counts, count => Assert.InRange(count, MinProductsPerCategory, MaxProductsPerCategory));
        Assert.All(context.Products, product => Assert.True(product.Price > 0));
    }
}
