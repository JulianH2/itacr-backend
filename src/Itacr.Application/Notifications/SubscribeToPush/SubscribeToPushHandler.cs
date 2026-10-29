using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;

namespace Itacr.Application.Notifications.SubscribeToPush;

/// <summary>
/// A waiter accepts notifications on their phone (HU_09): the browser's subscription is saved for them, so the orders they take are
/// announced there when the kitchen marks them as Ready. The same browser saved again replaces its keys, and moves to the waiter who
/// signed in last on it.
/// </summary>
public sealed class SubscribeToPushHandler(
    IPushSubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider,
    IValidator<PushSubscriptionCommand> validator)
{
    public async Task<Result> HandleAsync(PushSubscriptionCommand command, Guid employeeId, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        await subscriptionRepository.SaveForEmployeeAsync(employeeId, command.ToKeys(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}
