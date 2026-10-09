using System.Net;

namespace Itacr.Api.Tests;

/// <summary>Endpoints that must answer without a token, while the API stays closed by default.</summary>
public sealed class AnonymousEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/openapi/v1.json", "application/json")]
    [InlineData("/scalar/v1", "text/html")]
    public async Task Get_ShouldServeTheApiDocumentationWithoutAToken_WhenEnvironmentIsDevelopment(string url, string mediaType)
    {
        // Act
        using var response = await factory.CreateClient().GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ShouldRequireAToken_WhenTheEndpointIsNotMarkedAnonymous()
    {
        // Act
        using var response = await factory.CreateClient().GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
