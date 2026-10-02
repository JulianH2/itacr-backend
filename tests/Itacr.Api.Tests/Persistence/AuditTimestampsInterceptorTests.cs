using Itacr.Api.Features.Customers;
using Itacr.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Persistence;

public sealed class AuditTimestampsInterceptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Stamp_ShouldSetCreatedAtAndUpdatedAt_WhenEntityIsAdded()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();
        var customer = new Customer { Name = "Ana", Phone = "7731234567" };
        context.Add(customer);
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        AuditTimestampsInterceptor.Stamp(context.ChangeTracker, timeProvider.GetUtcNow().UtcDateTime);

        // Assert
        Assert.Equal(Now.UtcDateTime, customer.CreatedAt);
        Assert.Equal(Now.UtcDateTime, customer.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, customer.CreatedAt.Kind);
    }

    [Fact]
    public void Stamp_ShouldKeepCreatedAt_WhenCallerAlreadySetIt()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();
        var originalCreatedAt = Now.UtcDateTime.AddHours(-2);
        var customer = new Customer { Name = "Ana", Phone = "7731234567", CreatedAt = originalCreatedAt };
        context.Add(customer);

        // Act
        AuditTimestampsInterceptor.Stamp(context.ChangeTracker, Now.UtcDateTime);

        // Assert
        Assert.Equal(originalCreatedAt, customer.CreatedAt);
        Assert.Equal(Now.UtcDateTime, customer.UpdatedAt);
    }

    [Fact]
    public void Stamp_ShouldOnlyChangeUpdatedAt_WhenEntityIsModified()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();
        var createdAt = Now.UtcDateTime.AddDays(-1);
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Ana", Phone = "7731234567", CreatedAt = createdAt, UpdatedAt = createdAt };
        context.Attach(customer);
        customer.Name = "Ana María";

        // Act
        AuditTimestampsInterceptor.Stamp(context.ChangeTracker, Now.UtcDateTime);

        // Assert
        Assert.Equal(createdAt, customer.CreatedAt);
        Assert.Equal(Now.UtcDateTime, customer.UpdatedAt);
    }
}
