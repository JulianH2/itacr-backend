using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetActiveOrders;

/// <summary>
/// The orders the waiter took that are not delivered yet, newest first. A waiter only ever sees their own, and the delivered ones
/// leave the list.
/// </summary>
public sealed class GetActiveOrdersHandler(IOrderRepository orderRepository)
{
    /// <summary>A waiter does not have more active orders than this at once; it only bounds the answer.</summary>
    private const int MaxOrders = 50;

    public async Task<Result<IReadOnlyList<ActiveOrderResponse>>> HandleAsync(Guid waiterId, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetActiveByWaiterAsync(waiterId, MaxOrders, cancellationToken);

        return Result<IReadOnlyList<ActiveOrderResponse>>.Success(
            [.. orders.Select(order => new ActiveOrderResponse(order.Id, order.OrderNumber, order.Channel, order.Status, order.TableNumber, order.CreatedAt, order.LastStatusChangeAt))]);
    }
}
