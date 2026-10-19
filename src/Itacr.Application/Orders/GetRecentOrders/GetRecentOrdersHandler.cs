using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetRecentOrders;

/// <summary>The last orders the waiter sent during the current shift (the last 12 hours), newest first.</summary>
public sealed class GetRecentOrdersHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
{
    private const int MaxOrders = 10;
    private static readonly TimeSpan Shift = TimeSpan.FromHours(12);

    public async Task<Result<IReadOnlyList<RecentOrderResponse>>> HandleAsync(Guid waiterId, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow().UtcDateTime - Shift;
        var orders = await orderRepository.GetRecentByWaiterAsync(waiterId, since, MaxOrders, cancellationToken);

        return Result<IReadOnlyList<RecentOrderResponse>>.Success(
            [.. orders.Select(order => new RecentOrderResponse(order.Id, order.OrderNumber, order.Channel, order.Status, order.TableNumber, order.CreatedAt))]);
    }
}
