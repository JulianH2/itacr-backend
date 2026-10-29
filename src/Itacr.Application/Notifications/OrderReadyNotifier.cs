namespace Itacr.Application.Notifications;

/// <summary>
/// Sends the "order ready" notice (HU_09) to whoever is responsible for the order: the waiter who took it, with the order number
/// and the table, or the customer of a pickup order, with the order number only (never their name or phone). One notice per order,
/// and only to the browsers that accepted notifications; the others see the change when they open the app (CA4). The link carries
/// the notification id, so the PWA records it as read when the tap opens it, even if the app was closed.
/// </summary>
public sealed class OrderReadyNotifier(
    INotificationRepository notificationRepository,
    IPushSubscriptionRepository subscriptionRepository,
    IPushSender pushSender,
    TimeProvider timeProvider)
{
    private const string Title = "Pedido listo";
    private const string PickupOrdersUrl = "/pickup/orders";

    public async Task NotifyAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (await notificationRepository.CreateOrderReadyAsync(orderId, now, cancellationToken) is not { } target)
        {
            return;
        }

        var subscriptions = await subscriptionRepository.GetByRecipientAsync(target.WaiterId, target.WaiterId is null ? target.CustomerId : null, cancellationToken);
        var message = target.WaiterId is null
            ? new PushMessage(Title, $"Tu pedido #{target.OrderNumber} está listo. Ya puedes pasar por él.", WithNotification(PickupOrdersUrl, target), target.NotificationId)
            : new PushMessage(Title, $"Orden #{target.OrderNumber} · {Destination(target)} está lista para entregar.", WithNotification($"/orders/detail/{orderId}", target), target.NotificationId);

        foreach (var subscription in subscriptions)
        {
            if (await pushSender.SendAsync(subscription, message, cancellationToken) == PushDelivery.Gone)
            {
                await subscriptionRepository.DeleteAsync(subscription.Endpoint, cancellationToken);
            }
        }
    }

    private static string WithNotification(string path, OrderReadyTarget target) => $"{path}?notification={target.NotificationId}";

    private static string Destination(OrderReadyTarget target) => target.TableNumber is { } table ? $"Mesa {table}" : "Para llevar";
}
