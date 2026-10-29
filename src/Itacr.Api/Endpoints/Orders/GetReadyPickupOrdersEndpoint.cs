using Itacr.Api.Common.Http;
using Itacr.Application.Orders.GetActiveOrders;
using Itacr.Application.Orders.GetReadyPickupOrders;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders/pickups/ready")]
public sealed class GetReadyPickupOrdersEndpoint(GetReadyPickupOrdersHandler handler) : ControllerBase
{
    /// <summary>Lists the pickup orders of customers that are Ready and wait to be handed over, the longest waiting first.</summary>
    /// <remarks>
    /// They have no waiter, so they are not part of <c>GET /orders/active</c>. A waiter opens one to see its products and marks it as
    /// delivered when the customer arrives.
    /// </remarks>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The ready pickup orders; empty when none is waiting.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a waiter.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ActiveOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ActiveOrderResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
