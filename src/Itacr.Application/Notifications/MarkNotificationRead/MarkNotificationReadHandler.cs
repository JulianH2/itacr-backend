using Itacr.Application.Common.Results;

namespace Itacr.Application.Notifications.MarkNotificationRead;

/// <summary>
/// The phone reports that its notification was opened (HU_09: "registro de la notificación como leída"). The id travels inside the
/// notification and is a random GUID, so only who received it knows it. Only the first time is recorded, and an unknown id changes
/// nothing, so the answer never tells whether a notification exists.
/// </summary>
public sealed class MarkNotificationReadHandler(INotificationRepository notificationRepository, TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkReadAsync(notificationId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}
