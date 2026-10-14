using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders;
using Itacr.Application.Orders.UndoOrderStatusChange;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Kitchen) + "," + nameof(EmployeeRole.Manager))]
[Tags("Orders")]
[Route("api/v1/orders/{id:guid}/status/undo")]
public sealed class UndoOrderStatusChangeEndpoint(UndoOrderStatusChangeHandler handler) : ControllerBase
{
    /// <summary>Undoes the last kitchen status change of an order, for an accidental tap.</summary>
    /// <remarks>
    /// Only the employee who made the change can undo it, and only a few seconds after it. The order goes back to the status it
    /// had before; the undo is recorded in the history too.
    /// </remarks>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order is back in its previous status.</response>
    /// <response code="404">The order does not exist.</response>
    /// <response code="409">There is nothing to undo any more, or another employee made the change.</response>
    [HttpPost]
    [ProducesResponseType<OrderStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderStatusResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var employeeId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(id, employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
