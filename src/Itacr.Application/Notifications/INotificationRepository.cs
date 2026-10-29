using Itacr.Domain.Notifications;

namespace Itacr.Application.Notifications;

public interface INotificationRepository
{
    /// <summary>Inserts the notification; its <c>Id</c> is assigned by the repository.</summary>
    Task CreateAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>
    /// Records the "order ready" notice of an order that is Ready, once per order, and returns who receives it; null when the order
    /// is no longer Ready or its notice was already recorded.
    /// </summary>
    Task<OrderReadyTarget?> CreateOrderReadyAsync(Guid orderId, DateTime now, CancellationToken cancellationToken);

    Task MarkReadAsync(Guid notificationId, DateTime now, CancellationToken cancellationToken);
}
