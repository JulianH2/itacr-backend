using Itacr.Application.Common.Results;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.DeliverOrder;

/// <summary>
/// A waiter hands over an order that is Ready, to the table or to the customer who arrives, and closes it. Only the waiter who took
/// the order can do it, or any waiter for a pickup order of a customer. An order that is not Ready cannot be delivered, and a delivered
/// one is final. When two people deliver the same order at once, the second one gets a conflict.
/// </summary>
public sealed class DeliverOrderHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
{
    public async Task<Result<DeliverOrderResponse>> HandleAsync(Guid orderId, Guid waiterId, CancellationToken cancellationToken)
    {
        // The order of another waiter answers "not found", like an id that does not exist.
        if (await orderRepository.GetStatusAsync(orderId, cancellationToken) is not { } order
            || (order.WaiterId is { } takenBy && takenBy != waiterId))
        {
            return OrdersErrors.OrderNotFound;
        }

        if (order.Status == OrderStatus.Delivered)
        {
            return OrdersErrors.OrderAlreadyDelivered;
        }

        if (order.Status != OrderStatus.Ready)
        {
            return OrdersErrors.OrderNotReady;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (!await orderRepository.ChangeStatusAsync(orderId, OrderStatus.Ready, OrderStatus.Delivered, waiterId, now, cancellationToken))
        {
            return OrdersErrors.StatusAlreadyChanged;
        }

        return new DeliverOrderResponse(orderId, OrderStatus.Delivered, now);
    }
}
