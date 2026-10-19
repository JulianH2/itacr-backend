using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetPickupOrderStatus;

/// <summary>
/// The customer follows a pickup order with the <c>clientOrderId</c> their phone kept. It answers only pickup orders (a
/// waiter order is "not found", like an id that does not exist) and only with what the customer may see.
/// </summary>
public sealed class GetPickupOrderStatusHandler(IPickupOrderRepository pickupOrderRepository)
{
    public async Task<Result<PickupOrderResponse>> HandleAsync(Guid clientOrderId, CancellationToken cancellationToken)
    {
        if (await pickupOrderRepository.GetByClientOrderIdAsync(clientOrderId, cancellationToken) is not { } order)
        {
            return OrdersErrors.OrderNotFound;
        }

        return PickupOrderResponse.From(order);
    }
}
