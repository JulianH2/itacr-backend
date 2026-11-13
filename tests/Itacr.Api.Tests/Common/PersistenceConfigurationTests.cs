using Itacr.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Api.Tests.Common;

/// <summary>The database connection of the API (ITACR-66): without it the API cannot start, and it says why.</summary>
public sealed class PersistenceConfigurationTests
{
    [Fact]
    public void AddPersistence_ShouldFailWithTheNameOfTheSetting_WhenTheConnectionStringIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddPersistence(configuration));

        // Assert
        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void AddPersistence_ShouldRegisterTheRepositories_WhenTheConnectionStringIsThere()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Server=localhost;Database=Itacr;Trusted_Connection=True;" })
            .Build();

        // Act
        services.AddPersistence(configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<Itacr.Application.Orders.IOrderRepository>());
        Assert.NotNull(provider.GetService<Itacr.Application.Identity.IEmployeeRepository>());
    }
}
