using Itacr.Application.Common.Results;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Notifications.GetPushPublicKey;

public sealed record PushPublicKeyResponse(string PublicKey);

/// <summary>The VAPID public key the PWA needs to subscribe a browser (HU_09). It is public by design; "not found" means push is off.</summary>
public sealed class GetPushPublicKeyHandler(IOptions<WebPushOptions> options)
{
    public Result<PushPublicKeyResponse> Handle()
    {
        var settings = options.Value;
        return settings.IsConfigured ? new PushPublicKeyResponse(settings.PublicKey!) : NotificationsErrors.PushNotConfigured;
    }
}
