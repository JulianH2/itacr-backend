using Itacr.Domain.Notifications;

namespace Itacr.Application.Notifications;

public interface INotificationRepository
{
    /// <summary>Inserts the notification; its <c>Id</c> is assigned by the repository.</summary>
    Task CreateAsync(Notification notification, CancellationToken cancellationToken);
}
