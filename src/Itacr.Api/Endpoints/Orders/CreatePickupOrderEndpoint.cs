using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Orders;
using Itacr.Application.Orders.CreatePickupOrder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Orders;

/// <summary>A customer without an account sends an order to pick up. Public, so it has its own rate limit and rules.</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PublicOrdersServiceCollectionExtensions.CreateRateLimitPolicy)]
[Tags("Pickup orders")]
[Route("api/v1/public/orders")]
public sealed class CreatePickupOrderEndpoint(CreatePickupOrderHandler handler) : ControllerBase
{
    /// <summary>Sends a pickup order to the kitchen.</summary>
    /// <remarks>
    /// No login. The customer gives a name and a phone of 10 digits; the order enters the kitchen queue as "Para recoger" and is
    /// paid at pickup. Idempotent on <c>clientOrderId</c>: sending the same order again returns the order already saved with 200
    /// instead of creating a duplicate. Prices come from the menu, never from the request, and a table, a waiter, or a channel in
    /// the request are ignored. A phone can have a limited number of active orders (<c>PublicOrders</c> options). The answer has the
    /// order number and never the name or the phone.
    /// </remarks>
    /// <response code="201">The order was saved with status New.</response>
    /// <response code="200">The order had already been saved; it is returned as it is.</response>
    /// <response code="400">Invalid name, phone, or products; the errors are under <c>name</c>, <c>phone</c>, and <c>items</c>.</response>
    /// <response code="409">The phone already has the most active orders, or the clientOrderId belongs to another kind of order.</response>
    /// <response code="429">Too many orders from this address in a short time.</response>
    [HttpPost]
    [ProducesResponseType<PickupOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PickupOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PickupOrderResponse>> HandleAsync(CreatePickupOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        var outcome = result.Value;
        return outcome.Created ? StatusCode(StatusCodes.Status201Created, outcome.Order) : Ok(outcome.Order);
    }
}
