using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetOrderDetail;

/// <summary>The detail of an order with the timeline of its statuses, so the waiter sees where the kitchen is with it.</summary>
public sealed class GetOrderDetailHandler(IOrderRepository orderRepository)
{
    /// <param name="orderId">The order to read.</param>
    /// <param name="onlyWaiterId">
    /// When set, a waiter can read the orders they took and the pickup orders of customers (they hand those over, HU_10); the order of
    /// another waiter answers "not found" exactly like an id that does not exist. Kitchen staff and managers pass null and read any order.
    /// </param>
    /// <param name="cancellationToken">Aborts the request.</param>
    public async Task<Result<OrderDetailResponse>> HandleAsync(Guid orderId, Guid? onlyWaiterId, CancellationToken cancellationToken)
    {
        if (await orderRepository.GetDetailAsync(orderId, cancellationToken) is not { } order
            || (onlyWaiterId is { } waiterId && order.WaiterId is { } takenBy && takenBy != waiterId))
        {
            return OrdersErrors.OrderNotFound;
        }

        return new OrderDetailResponse(
            order.Id,
            order.OrderNumber,
            order.Channel,
            order.Status,
            order.TableNumber,
            order.Total,
            order.CreatedAt,
            order.ReadyAt,
            order.DeliveredAt,
            [.. order.Items.Select(item => new OrderDetailItemResponse(item.Name, item.Quantity, item.UnitPrice, item.Subtotal))],
            [.. order.History.Select(change => new OrderTimelineEntryResponse(change.ToStatus, change.ChangedAt, change.FromStatus > change.ToStatus))]);
    }
}
