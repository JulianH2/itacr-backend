using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Notifications.MarkNotificationRead;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Notifications;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PublicOrdersServiceCollectionExtensions.StatusRateLimitPolicy)]
[Tags("Notifications")]
[Route("api/v1/public/notifications/{id:guid}/read")]
public sealed class MarkNotificationReadEndpoint(MarkNotificationReadHandler handler) : ControllerBase
{
    /// <summary>Records that the notification was opened.</summary>
    /// <remarks>
    /// Public because the customer has no session; the id only travels inside the notification. Only the first time is recorded, and
    /// the answer is the same for an unknown id, so it never tells whether a notification exists.
    /// </remarks>
    /// <param name="id">The notification id from the notification's data.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">Recorded, or nothing to record.</response>
    /// <response code="429">Too many requests from this address in a short time.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
