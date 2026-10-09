using System.Net;
using Itacr.Api.Tests.Auth;

namespace Itacr.Api.Tests;

public sealed class HealthCheckTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Get_ShouldReturnHealthyWithoutAToken_WhenTheApiCanRunAProcedure()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
