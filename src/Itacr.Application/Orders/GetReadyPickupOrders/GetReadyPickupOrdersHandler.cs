using Itacr.Application.Common.Results;
using Itacr.Application.Orders.GetActiveOrders;

namespace Itacr.Application.Orders.GetReadyPickupOrders;

/// <summary>
/// The pickup orders of customers that are Ready and wait for the customer to arrive, the longest waiting first, so a waiter finds
/// the one to hand over. They have no waiter, so they are not part of anyone's active orders.
/// </summary>
public sealed class GetReadyPickupOrdersHandler(IOrderRepository orderRepository)
{
    /// <summary>A customer has at most a few active pickup orders; this only bounds the answer.</summary>
    private const int MaxOrders = 50;

    public async Task<Result<IReadOnlyList<ActiveOrderResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetReadyPickupsAsync(MaxOrders, cancellationToken);

        return Result<IReadOnlyList<ActiveOrderResponse>>.Success(
            [.. orders.Select(order => new ActiveOrderResponse(order.Id, order.OrderNumber, order.Channel, order.Status, order.TableNumber, order.CreatedAt, order.LastStatusChangeAt))]);
    }
}
