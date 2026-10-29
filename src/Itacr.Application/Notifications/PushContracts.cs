namespace Itacr.Application.Notifications;

/// <summary>A browser that accepted notifications: where to send them and the keys to encrypt them for that browser.</summary>
public sealed record PushSubscriptionKeys(string Endpoint, string P256dh, string Auth);

/// <summary>What the phone shows. <see cref="Url"/> is the page that opens when the notification is tapped.</summary>
public sealed record PushMessage(string Title, string Body, string Url, Guid NotificationId);

public enum PushDelivery
{
    Sent,

    /// <summary>The push service says the subscription no longer exists (404 or 410); it is deleted.</summary>
    Gone,

    Failed,
}

/// <summary>Sends one notification to one browser through its push service.</summary>
public interface IPushSender
{
    Task<PushDelivery> SendAsync(PushSubscriptionKeys subscription, PushMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Orders that just became Ready and wait for their notice. The kitchen's request only enqueues, so a slow push service never
/// delays the tap; a background service sends them.
/// </summary>
public interface IOrderReadyQueue
{
    void Enqueue(Guid orderId);
}

public interface IPushSubscriptionRepository
{
    Task SaveForEmployeeAsync(Guid employeeId, PushSubscriptionKeys subscription, DateTime now, CancellationToken cancellationToken);

    /// <summary>Saves it for the customer of the pickup order; false when there is no pickup order with that id.</summary>
    Task<bool> SaveForPickupOrderAsync(Guid clientOrderId, PushSubscriptionKeys subscription, DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<PushSubscriptionKeys>> GetByRecipientAsync(Guid? employeeId, Guid? customerId, CancellationToken cancellationToken);

    Task DeleteAsync(string endpoint, CancellationToken cancellationToken);
}

/// <summary>The order whose "ready" notice was just recorded, and who receives it: its waiter or the customer of a pickup order.</summary>
public sealed record OrderReadyTarget(Guid NotificationId, int OrderNumber, string Channel, int? TableNumber, Guid? WaiterId, Guid? CustomerId);
