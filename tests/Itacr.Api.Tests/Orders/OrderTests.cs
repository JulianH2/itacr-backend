using Itacr.Domain.Orders;

namespace Itacr.Api.Tests.Orders;

public sealed class OrderTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly OrderStatus[] StatusFlow = [OrderStatus.New, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.Delivered];

    public static TheoryData<OrderStatus, OrderStatus, bool> Transitions
    {
        get
        {
            var data = new TheoryData<OrderStatus, OrderStatus, bool>();
            foreach (var from in StatusFlow)
            {
                foreach (var to in StatusFlow)
                {
                    data.Add(from, to, Array.IndexOf(StatusFlow, to) == Array.IndexOf(StatusFlow, from) + 1);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Transitions))]
    public void CanChange_ShouldAllowOnlyTheNextStep_WhenAStatusIsRequested(OrderStatus from, OrderStatus to, bool expected)
    {
        // Act
        var allowed = OrderStatusRules.CanChange(from, to);

        // Assert
        Assert.Equal(expected, allowed);
    }

    [Fact]
    public void Create_ShouldStartAsNewWithItsFirstHistoryRow_WhenTheOrderIsPlaced()
    {
        // Arrange
        var waiterId = Guid.NewGuid();

        // Act
        var order = Order.Create(Guid.NewGuid(), OrderChannel.DineIn, Guid.NewGuid(), waiterId, null, Now);

        // Assert
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(0m, order.Total);
        Assert.Equal(Now, order.LastStatusChangeAt);
        var first = Assert.Single(order.StatusHistory);
        Assert.Null(first.FromStatus);
        Assert.Equal(OrderStatus.New, first.ToStatus);
        Assert.Equal(waiterId, first.ChangedById);
    }

    [Theory]
    [InlineData(OrderChannel.DineIn, false)]
    [InlineData(OrderChannel.TakeAway, true)]
    public void Create_ShouldReject_WhenTheTableDoesNotMatchTheChannel(OrderChannel channel, bool withTable)
    {
        // Arrange
        Guid? tableId = withTable ? Guid.NewGuid() : null;

        // Act
        var create = () => Order.Create(Guid.NewGuid(), channel, tableId, null, null, Now);

        // Assert
        Assert.Equal("tableId", Assert.Throws<ArgumentException>(create).ParamName);
    }

    [Theory]
    [InlineData(OrderChannel.DineIn, true)]
    [InlineData(OrderChannel.TakeAway, false)]
    [InlineData(OrderChannel.MobileApp, false)]
    public void Create_ShouldAccept_WhenTheTableMatchesTheChannel(OrderChannel channel, bool withTable)
    {
        // Arrange
        Guid? tableId = withTable ? Guid.NewGuid() : null;

        // Act
        var order = Order.Create(Guid.NewGuid(), channel, tableId, null, null, Now);

        // Assert
        Assert.Equal(tableId, order.TableId);
    }

    [Fact]
    public void Children_ShouldLeaveTheOrderIdToTheRepository_WhenItemsAndHistoryAreCreated()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.TakeAway, null, null, null, Now);

        // Act
        var item = order.AddItem(Guid.NewGuid(), 1, 10m, null, null, [], [], Now);
        order.TryChangeStatus(OrderStatus.InPreparation, null, Now);

        // Assert
        Assert.Equal(Guid.Empty, item.OrderId);
        Assert.All(order.StatusHistory, entry => Assert.Equal(Guid.Empty, entry.OrderId));
    }

    [Fact]
    public void AddItem_ShouldCopyThePriceAndRecalculateTheTotal_WhenItemsAreAdded()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.TakeAway, null, null, null, Now);

        // Act
        var burger = order.AddItem(Guid.NewGuid(), 2, 89.50m, "Grande", null, ["Queso"], [], Now);
        order.AddItem(Guid.NewGuid(), 1, 32m, null, null, [], ["Hielo"], Now);

        // Assert
        Assert.Equal(179m, burger.Subtotal);
        Assert.Equal(211m, order.Total);
        Assert.Equal(["Queso"], burger.Extras);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, -0.01)]
    public void AddItem_ShouldReject_WhenQuantityOrPriceIsInvalid(int quantity, double unitPrice)
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.TakeAway, null, null, null, Now);

        // Act
        var add = () => order.AddItem(Guid.NewGuid(), quantity, (decimal)unitPrice, null, null, [], [], Now);

        // Assert
        Assert.ThrowsAny<ArgumentOutOfRangeException>(add);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void TryChangeStatus_ShouldRecordEachStepAndItsTimestamps_WhenTheOrderAdvances()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.DineIn, Guid.NewGuid(), Guid.NewGuid(), null, Now);
        var cook = Guid.NewGuid();
        var readyAt = Now.AddMinutes(8);
        var deliveredAt = Now.AddMinutes(10);

        // Act
        var started = order.TryChangeStatus(OrderStatus.InPreparation, cook, Now.AddMinutes(1));
        var ready = order.TryChangeStatus(OrderStatus.Ready, cook, readyAt);
        var delivered = order.TryChangeStatus(OrderStatus.Delivered, order.WaiterId, deliveredAt);

        // Assert
        Assert.True(started && ready && delivered);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(readyAt, order.ReadyAt);
        Assert.Equal(deliveredAt, order.DeliveredAt);
        Assert.Equal(deliveredAt, order.LastStatusChangeAt);
        Assert.Equal(
            [(null, OrderStatus.New), (OrderStatus.New, OrderStatus.InPreparation), (OrderStatus.InPreparation, OrderStatus.Ready), (OrderStatus.Ready, OrderStatus.Delivered)],
            order.StatusHistory.Select(entry => (entry.FromStatus, entry.ToStatus)));
    }

    [Theory]
    [InlineData(OrderStatus.New, OrderStatus.Ready)]
    [InlineData(OrderStatus.New, OrderStatus.New)]
    [InlineData(OrderStatus.Ready, OrderStatus.InPreparation)]
    public void TryChangeStatus_ShouldChangeNothing_WhenTheStepIsNotAllowed(OrderStatus current, OrderStatus requested)
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.DineIn, Guid.NewGuid(), null, null, Now);
        order.Status = current;
        var historyCount = order.StatusHistory.Count;

        // Act
        var changed = order.TryChangeStatus(requested, null, Now.AddMinutes(1));

        // Assert
        Assert.False(changed);
        Assert.Equal(current, order.Status);
        Assert.Equal(Now, order.LastStatusChangeAt);
        Assert.Equal(historyCount, order.StatusHistory.Count);
        Assert.Null(order.ReadyAt);
    }

    [Fact]
    public void TryChangeStatus_ShouldRejectEveryChange_WhenTheOrderIsDelivered()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), OrderChannel.DineIn, Guid.NewGuid(), null, null, Now);
        order.Status = OrderStatus.Delivered;

        // Act
        var results = StatusFlow.Select(status => order.TryChangeStatus(status, null, Now));

        // Assert
        Assert.All(results, changed => Assert.False(changed));
    }
}
