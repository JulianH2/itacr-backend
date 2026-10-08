using Itacr.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Tests.Persistence;

public sealed class ModelConventionsTests
{
    [Fact]
    public void Model_ShouldHaveUniqueIndexOnClientOrderId_WhenOrderIsConfigured()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();

        // Act
        var order = context.Model.FindEntityType(typeof(Order))!;

        // Assert
        Assert.Contains(order.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Order.ClientOrderId));
    }

    [Fact]
    public void Model_ShouldStoreEnumsAsText_WhenOrderIsConfigured()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();
        var order = context.Model.FindEntityType(typeof(Order))!;

        // Act
        var status = order.FindProperty(nameof(Order.Status))!;

        // Assert
        Assert.Equal(typeof(string), status.GetProviderClrType());
        Assert.NotNull(status.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldUseDecimalPrecisionForMoney_WhenOrderItemIsConfigured()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();
        var item = context.Model.FindEntityType(typeof(OrderItem))!;

        // Act
        var unitPrice = item.FindProperty(nameof(OrderItem.UnitPrice))!;

        // Assert
        Assert.Equal(10, unitPrice.GetPrecision());
        Assert.Equal(2, unitPrice.GetScale());
    }

    [Fact]
    public void Model_ShouldCascadeDeleteOnlyFromOrder_WhenForeignKeysAreConfigured()
    {
        // Arrange
        using var context = PersistenceTestContext.Create();

        // Act
        var cascades = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(foreignKey => foreignKey.PrincipalEntityType.ClrType)
            .ToList();

        // Assert
        Assert.Equal(2, cascades.Count);
        Assert.All(cascades, principal => Assert.Equal(typeof(Order), principal));
    }
}
