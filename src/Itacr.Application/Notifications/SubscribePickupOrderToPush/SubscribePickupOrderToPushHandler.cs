using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Application.Orders;

namespace Itacr.Application.Notifications.SubscribePickupOrderToPush;

/// <summary>
/// A customer without an account accepts notifications while following a pickup order (HU_09 CA2). The subscription belongs to the
/// customer of that order, found with the receipt their phone kept; an id that is not a pickup order answers "not found".
/// </summary>
public sealed class SubscribePickupOrderToPushHandler(
    IPushSubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider,
    IValidator<PushSubscriptionCommand> validator)
{
    public async Task<Result> HandleAsync(Guid clientOrderId, PushSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await subscriptionRepository.SaveForPickupOrderAsync(clientOrderId, command.ToKeys(), now, cancellationToken)
            ? Result.Success()
            : OrdersErrors.OrderNotFound;
    }
}
