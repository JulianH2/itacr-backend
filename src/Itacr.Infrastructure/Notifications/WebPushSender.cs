using System.Net;
using System.Text.Json;
using Itacr.Application.Notifications;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LibPushMessage = Lib.Net.Http.WebPush.PushMessage;
using LibPushSubscription = Lib.Net.Http.WebPush.PushSubscription;
using PushMessage = Itacr.Application.Notifications.PushMessage;

namespace Itacr.Infrastructure.Notifications;

/// <summary>
/// Sends Web Push messages signed with the VAPID keys (RFC 8292) and encrypted for each browser (RFC 8291). The payload follows the
/// format of the Angular service worker, so tapping the notification opens <see cref="PushMessage.Url"/> even with the app closed.
/// </summary>
internal sealed class WebPushSender(HttpClient httpClient, IOptions<WebPushOptions> options, ILogger<WebPushSender> logger) : IPushSender
{
    private const string Icon = "/icons/icon-192x192.png";
    private const int TimeToLiveSeconds = 3600;

    private static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web);

    public async Task<PushDelivery> SendAsync(PushSubscriptionKeys subscription, PushMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning("Web Push is not configured (WebPush:PublicKey and WebPush:PrivateKey); notification {NotificationId} was not sent", message.NotificationId);
            return PushDelivery.Failed;
        }

        var client = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = new VapidAuthentication(settings.PublicKey, settings.PrivateKey) { Subject = settings.Subject },
        };
        var target = new LibPushSubscription { Endpoint = subscription.Endpoint };
        target.SetKey(PushEncryptionKeyName.P256DH, subscription.P256dh);
        target.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);

        try
        {
            await client.RequestPushMessageDeliveryAsync(
                target,
                new LibPushMessage(Payload(message)) { TimeToLive = TimeToLiveSeconds, Urgency = PushMessageUrgency.High },
                cancellationToken);
            return PushDelivery.Sent;
        }
        catch (PushServiceClientException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return PushDelivery.Gone;
        }
        catch (Exception exception) when (exception is PushServiceClientException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Push service rejected notification {NotificationId}: {Message}", message.NotificationId, exception.Message);
            return PushDelivery.Failed;
        }
    }

    private static string Payload(PushMessage message) => JsonSerializer.Serialize(
        new
        {
            notification = new
            {
                title = message.Title,
                body = message.Body,
                icon = Icon,
                tag = $"order-ready-{message.NotificationId}",
                data = new
                {
                    notificationId = message.NotificationId,
                    onActionClick = new { @default = new { operation = "navigateLastFocusedOrOpen", url = message.Url } },
                },
            },
        },
        PayloadJson);
}
