using System.Net;
using Itacr.Api.Tests.Auth;

namespace Itacr.Api.Tests;

public sealed class HealthCheckTests
{
    [Fact]
    public async Task Get_ShouldReturnHealthyWithoutAToken_WhenTheDatabaseIsReachable()
    {
        // Arrange
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
