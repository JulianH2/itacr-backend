using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Notifications;
using Itacr.Application.Notifications.SubscribeToPush;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Notifications;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Notifications")]
[Route("api/v1/push/subscriptions")]
public sealed class SubscribeToPushEndpoint(SubscribeToPushHandler handler) : ControllerBase
{
    /// <summary>Saves the push subscription of the waiter's browser, so their orders are announced there when they are Ready.</summary>
    /// <remarks>
    /// The body is the subscription <c>PushManager.subscribe</c> returns (endpoint and the keys <c>p256dh</c> and <c>auth</c>). Saving the
    /// same browser again replaces its keys; a browser another waiter used before now belongs to this one.
    /// </remarks>
    /// <param name="command">The browser's push subscription.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The subscription is not valid.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a waiter.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HandleAsync(PushSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(command, waiterId, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
