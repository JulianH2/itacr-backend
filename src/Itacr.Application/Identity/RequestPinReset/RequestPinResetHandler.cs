using Itacr.Application.Common.Persistence;
using Itacr.Application.Common.Results;
using Itacr.Application.Notifications;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;

namespace Itacr.Application.Identity.RequestPinReset;

/// <summary>An employee who forgot the PIN asks for a reset: every active manager is notified (or "any manager" when there is none).</summary>
public sealed class RequestPinResetHandler(
    IEmployeeRepository employeeRepository,
    INotificationRepository notificationRepository,
    IDbSession dbSession,
    TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(RequestPinResetCommand command, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee?.Status != EmployeeStatus.Active)
        {
            return IdentityErrors.EmployeeNotFound;
        }

        var managerIds = await employeeRepository.GetActiveManagerIdsAsync(command.EmployeeId, cancellationToken);
        IReadOnlyList<Guid?> recipients = managerIds.Count == 0 ? [null] : [.. managerIds.Select(id => (Guid?)id)];

        var sentAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbSession.ExecuteInTransactionAsync(
            async token =>
            {
                foreach (var recipientId in recipients)
                {
                    await notificationRepository.CreateAsync(
                        new Notification
                        {
                            Type = NotificationType.PinReset,
                            SubjectEmployeeId = command.EmployeeId,
                            RecipientEmployeeId = recipientId,
                            SentAt = sentAt,
                            CreatedAt = sentAt,
                            UpdatedAt = sentAt,
                        },
                        token);
                }
            },
            cancellationToken);
        return Result.Success();
    }
}
