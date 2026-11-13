using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Itacr.Api.Tests;

/// <summary>Outside Development the API forces HTTPS and tells the browsers to keep using it (HSTS).</summary>
public sealed class ProductionPipelineTests(ProductionPipelineTests.ProductionApiFactory factory)
    : IClassFixture<ProductionPipelineTests.ProductionApiFactory>
{
    private const string Host = "itacr.example";
    private const string Path = "/api/v1/auth/me";

    [Fact]
    public async Task Get_ShouldSendTheHstsHeader_WhenTheRequestIsHttpsInProduction()
    {
        // Arrange
        using var client = CreateClient("https");

        // Act
        using var response = await client.GetAsync(Path);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Strict-Transport-Security", out var values));
        Assert.Contains("max-age=", values.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_ShouldRedirectToHttps_WhenTheRequestIsHttpInProduction()
    {
        // Arrange
        using var client = CreateClient("http");

        // Act
        using var response = await client.GetAsync(Path);

        // Assert
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal($"https://{Host}{Path}", response.Headers.Location?.ToString());
    }

    private HttpClient CreateClient(string scheme) => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri($"{scheme}://{Host}"),
        AllowAutoRedirect = false,
    });

    /// <summary>The API in the Production environment, with the HTTPS port the redirection needs. No test opens a database connection.</summary>
    public sealed class ProductionApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("https_port", "443");
            builder.UseSetting("ConnectionStrings:Default", ApiFactory.PlaceholderConnectionString);
            builder.UseSetting("Jwt:Secret", ApiFactory.TestJwtSecret);
        }
    }
}
