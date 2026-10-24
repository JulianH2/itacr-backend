using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.GetOrderDetail;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter) + "," + nameof(EmployeeRole.Kitchen) + "," + nameof(EmployeeRole.Manager))]
[Tags("Orders")]
[Route("api/v1/orders/{id:guid}")]
public sealed class GetOrderDetailEndpoint(GetOrderDetailHandler handler) : ControllerBase
{
    /// <summary>Returns an order with its products and the timeline of its statuses.</summary>
    /// <remarks>Kitchen staff and managers can read any order. A waiter can only read the orders they took.</remarks>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order; <c>timeline</c> starts with New and lists every change, oldest first.</response>
    /// <response code="404">The order does not exist, or a waiter asked for an order of another waiter (same answer, so it is not revealed).</response>
    [HttpGet]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid? onlyWaiterId = null;
        if (User.IsInRole(nameof(EmployeeRole.Waiter)))
        {
            if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
            {
                return Unauthorized();
            }

            onlyWaiterId = waiterId;
        }

        var result = await handler.HandleAsync(id, onlyWaiterId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
