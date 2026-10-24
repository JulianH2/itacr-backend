using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.GetActiveOrders;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders/active")]
public sealed class GetActiveOrdersEndpoint(GetActiveOrdersHandler handler) : ControllerBase
{
    /// <summary>Lists the orders of the signed-in waiter that are not delivered yet, newest first.</summary>
    /// <remarks>
    /// Only the waiter's own orders: those in New, InPreparation, or Ready. Delivered orders leave the list, and the orders of other
    /// waiters and the pickup orders of customers are never part of it. The dashboard asks for it every few seconds.
    /// </remarks>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The active orders; empty when the waiter has none.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a waiter.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ActiveOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ActiveOrderResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(waiterId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
