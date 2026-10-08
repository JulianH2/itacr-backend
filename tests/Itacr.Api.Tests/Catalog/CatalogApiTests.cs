using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itacr.Application.Catalog;
using Itacr.Application.Catalog.Responses;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Api.Tests.Catalog;

public sealed class CatalogApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ProblemJsonMediaType = "application/problem+json";

    private static readonly CategoryResponse Combos = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Combos", 1);
    private static readonly CategoryResponse Drinks = new(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Bebidas", 4);
    private static readonly CategoryResponse Empty = new(Guid.Parse("10000000-0000-0000-0000-000000000009"), "Promociones", 9);

    private static readonly ProductResponse ClassicCombo = new(
        Guid.Parse("20000000-0000-0000-0000-000000000001"), Combos.Id, "Combo Clásico", "Hamburguesa, papas y refresco.", 119.00m, null, true);

    private static readonly ProductResponse Soda = new(
        Guid.Parse("20000000-0000-0000-0000-000000000012"), Drinks.Id, "Refresco de Cola", "Refresco de 355 ml.", 29.00m, "/assets/images/products/refresco-de-cola.png", false);

    private HttpClient CreateClient()
    {
        var fake = new FakeCatalogService([Combos, Drinks, Empty], [ClassicCombo, Soda]);
        return factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddScoped<ICatalogService>(_ => fake)))
            .CreateClient();
    }

    [Fact]
    public async Task GetCategories_ShouldReturnCategories_WhenCallerIsAnonymous()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/catalog/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<CategoryResponse[]>();
        Assert.Equal(["Combos", "Bebidas", "Promociones"], categories!.Select(category => category.Name));
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOnlyProductsOfTheCategory_WhenCategoryIdIsValid()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/v1/catalog/products?categoryId={Combos.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();
        Assert.Equal([ClassicCombo], products!);
    }

    [Fact]
    public async Task GetProducts_ShouldExposeTheDocumentedJsonShape_WhenCategoryHasProducts()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/v1/catalog/products?categoryId={Drinks.Id}");

        // Assert
        var product = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
        var propertyNames = product.EnumerateObject().Select(property => property.Name);
        Assert.Equal(["id", "categoryId", "name", "description", "price", "imageUrl", "isCombo"], propertyNames);
        Assert.Equal(JsonValueKind.Number, product.GetProperty("price").ValueKind);
        Assert.Equal(29m, product.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task GetProducts_ShouldReturnEmptyArray_WhenCategoryHasNoProducts()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/v1/catalog/products?categoryId={Empty.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/v1/catalog/products")]
    [InlineData("/api/v1/catalog/products?categoryId=not-a-guid")]
    public async Task GetProducts_ShouldReturnProblemDetails_WhenCategoryIdIsMissingOrInvalid(string url)
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
    }
}
