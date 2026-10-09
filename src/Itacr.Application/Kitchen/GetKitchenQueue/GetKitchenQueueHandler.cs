using Itacr.Application.Common.Results;

namespace Itacr.Application.Kitchen.GetKitchenQueue;

/// <summary>
/// The kitchen board: every active order, waiter and customer pickup alike, in one queue ordered by the time it was
/// created. An order synced late after being taken offline sits where its original time puts it.
/// </summary>
public sealed class GetKitchenQueueHandler(IKitchenRepository kitchenRepository, TimeProvider timeProvider)
{
    public async Task<Result<KitchenQueueResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var orders = await kitchenRepository.GetActiveOrdersAsync(cancellationToken);

        return new KitchenQueueResponse(
            timeProvider.GetUtcNow().UtcDateTime,
            [.. orders.OrderBy(order => order.CreatedAt).ThenBy(order => order.OrderNumber).Select(ToResponse)]);
    }

    private static KitchenQueueOrderResponse ToResponse(ActiveOrder order) => new(
        order.Id,
        order.OrderNumber,
        order.Channel,
        order.Status,
        order.TableNumber,
        order.CreatedAt,
        [.. order.Items.Select(item => new KitchenQueueItemResponse(item.ProductName, item.Quantity, item.Size, item.Drink, item.Extras, item.Exclusions))]);
}
