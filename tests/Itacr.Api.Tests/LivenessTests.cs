using System.Net;

namespace Itacr.Api.Tests;

/// <summary>The liveness route answers from the process alone, so it works with the database paused or unreachable.</summary>
public sealed class LivenessTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Get_ShouldReturnHealthyWithoutAToken_WhenTheDatabaseIsNotReachable()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldStillCheckTheDatabaseOnTheFullHealthRoute()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/health");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
