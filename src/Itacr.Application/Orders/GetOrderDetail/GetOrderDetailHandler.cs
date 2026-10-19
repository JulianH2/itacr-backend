using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetOrderDetail;

/// <summary>The detail of an order with the timeline of its statuses, so the waiter sees where the kitchen is with it.</summary>
public sealed class GetOrderDetailHandler(IOrderRepository orderRepository)
{
    public async Task<Result<OrderDetailResponse>> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (await orderRepository.GetDetailAsync(orderId, cancellationToken) is not { } order)
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
            [.. order.Items.Select(item => new OrderDetailItemResponse(item.Name, item.Quantity, item.UnitPrice, item.Subtotal))],
            [.. order.History.Select(change => new OrderTimelineEntryResponse(change.ToStatus, change.ChangedAt, change.FromStatus > change.ToStatus))]);
    }
}
