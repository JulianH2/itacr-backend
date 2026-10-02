using System.Net;

namespace Itacr.Api.Tests;

public sealed class ErrorResponseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ProblemJsonMediaType = "application/problem+json";

    [Fact]
    public async Task Get_ShouldReturnProblemDetails_WhenRouteDoesNotExist()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ShouldIncludeSecurityHeaders_WhenAnyRouteIsCalled()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/unknown");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }
}
