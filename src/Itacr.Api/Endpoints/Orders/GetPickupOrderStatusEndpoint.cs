using Itacr.Api.Common;
using Itacr.Api.Common.Http;
using Itacr.Application.Orders;
using Itacr.Application.Orders.GetPickupOrderStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Itacr.Api.Endpoints.Orders;

/// <summary>A customer follows a pickup order with the id their phone kept. Public and read-only.</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PublicOrdersServiceCollectionExtensions.StatusRateLimitPolicy)]
[Tags("Pickup orders")]
[Route("api/v1/public/orders/{clientOrderId:guid}")]
public sealed class GetPickupOrderStatusEndpoint(GetPickupOrderStatusHandler handler) : ControllerBase
{
    /// <summary>Returns the status of a pickup order.</summary>
    /// <remarks>
    /// Only orders sent through <c>POST /api/v1/public/orders</c> answer here. The id is a random GUID made by the customer's
    /// device, so it cannot be guessed; an id that is not a pickup order (for example a waiter's) answers 404 like one that does not
    /// exist. The answer never has the name or the phone of the customer.
    /// </remarks>
    /// <param name="clientOrderId">The id the customer's device generated when it sent the order.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order with its current status.</response>
    /// <response code="404">There is no pickup order with that id.</response>
    /// <response code="429">Too many queries from this address in a short time.</response>
    [HttpGet]
    [ProducesResponseType<PickupOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PickupOrderResponse>> HandleAsync(Guid clientOrderId, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(clientOrderId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
