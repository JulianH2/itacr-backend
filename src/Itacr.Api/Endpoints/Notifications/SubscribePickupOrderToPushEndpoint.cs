using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Notifications;
using Itacr.Application.Notifications.SubscribePickupOrderToPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Notifications;

/// <summary>A customer without an account asks to be told when the pickup order is ready. Public, limited like the status query.</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PublicOrdersServiceCollectionExtensions.StatusRateLimitPolicy)]
[Tags("Pickup orders")]
[Route("api/v1/public/orders/{clientOrderId:guid}/push-subscription")]
public sealed class SubscribePickupOrderToPushEndpoint(SubscribePickupOrderToPushHandler handler) : ControllerBase
{
    /// <summary>Saves the push subscription of the customer's browser for this pickup order and the next ones from the same phone.</summary>
    /// <remarks>
    /// The id is the receipt the customer's phone kept. The notice only says the order number is ready; it never carries the name or
    /// the phone. An id that is not a pickup order answers 404 like one that does not exist.
    /// </remarks>
    /// <param name="clientOrderId">The id the customer's device generated when it sent the order.</param>
    /// <param name="command">The browser's push subscription.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The subscription is not valid.</response>
    /// <response code="404">There is no pickup order with that id.</response>
    /// <response code="429">Too many requests from this address in a short time.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> HandleAsync(Guid clientOrderId, PushSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(clientOrderId, command, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
