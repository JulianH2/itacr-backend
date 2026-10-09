using System.Net;
using System.Net.Http.Json;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Catalog.Responses;

namespace Itacr.Api.Tests.Catalog;

/// <summary>The catalog endpoints against the real stored procedures and the catalog seeded by V001.</summary>
public sealed class CatalogDatabaseTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly Guid DrinksId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task GetCategories_ShouldReturnTheSeededCategoriesInDisplayOrder_WhenTheDatabaseIsMigrated()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        var categories = await client.GetFromJsonAsync<CategoryResponse[]>("/api/v1/catalog/categories", AuthApiFactory.Json);

        // Assert
        Assert.NotNull(categories);
        Assert.Equal(["Combos", "Hamburguesas", "Complementos", "Bebidas", "Postres"], categories.Select(category => category.Name));
        Assert.Equal([1, 2, 3, 4, 5], categories.Select(category => category.SortOrder));
    }

    [Fact]
    public async Task GetProducts_ShouldReturnTheCategoryProductsOrderedByName_WhenTheCategoryExists()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        var products = await client.GetFromJsonAsync<ProductResponse[]>($"/api/v1/catalog/products?categoryId={DrinksId}", AuthApiFactory.Json);

        // Assert
        Assert.NotNull(products);
        Assert.Equal(["Agua Natural", "Limonada", "Refresco de Cola", "Refresco de Lima-Limón"], products.Select(product => product.Name));
        Assert.All(products, product => Assert.Equal(DrinksId, product.CategoryId));
        Assert.Equal(29.00m, products.Single(product => product.Name == "Refresco de Cola").Price);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnAnEmptyArray_WhenTheCategoryDoesNotExist()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/v1/catalog/products?categoryId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }
}
