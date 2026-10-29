using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Notifications.GetPushPublicKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Notifications;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PublicOrdersServiceCollectionExtensions.StatusRateLimitPolicy)]
[Tags("Notifications")]
[Route("api/v1/public/push/public-key")]
public sealed class GetPushPublicKeyEndpoint(GetPushPublicKeyHandler handler) : ControllerBase
{
    /// <summary>Returns the VAPID public key the PWA passes to <c>PushManager.subscribe</c>.</summary>
    /// <remarks>The key is public by design; the private one never leaves the API. 404 means notifications are off in this environment.</remarks>
    /// <response code="200">The public key (base64url).</response>
    /// <response code="404">Notifications are not configured.</response>
    /// <response code="429">Too many requests from this address in a short time.</response>
    [HttpGet]
    [ProducesResponseType<PushPublicKeyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<PushPublicKeyResponse> Handle()
    {
        var result = handler.Handle();
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
