using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.DeliverOrder;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders/{id:guid}/deliver")]
public sealed class DeliverOrderEndpoint(DeliverOrderHandler handler) : ControllerBase
{
    /// <summary>Marks an order that is Ready as delivered, so it leaves the active lists and its delivery time is recorded.</summary>
    /// <remarks>
    /// The waiter can deliver the orders they took and the pickup orders of customers. Only an order in Ready can be delivered. The change
    /// and its time are recorded in the order history, and <c>deliveredAt</c> is set. A delivered order is final.
    /// </remarks>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order is delivered.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a waiter.</response>
    /// <response code="404">The order does not exist, or another waiter took it.</response>
    /// <response code="409">The order is not Ready yet, it was already delivered, or it changed while the request was in progress.</response>
    [HttpPost]
    [ProducesResponseType<DeliverOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliverOrderResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(id, waiterId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
