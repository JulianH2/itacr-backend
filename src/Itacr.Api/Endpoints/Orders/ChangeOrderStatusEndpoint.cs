using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders;
using Itacr.Application.Orders.ChangeOrderStatus;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Kitchen) + "," + nameof(EmployeeRole.Manager))]
[Tags("Orders")]
[Route("api/v1/orders/{id:guid}/status")]
public sealed class ChangeOrderStatusEndpoint(ChangeOrderStatusHandler handler) : ControllerBase
{
    /// <summary>Moves an order to its next kitchen status: New to InPreparation, or InPreparation to Ready.</summary>
    /// <remarks>
    /// Records the change and its time in the order history; becoming Ready also sets <c>readyAt</c>. Delivered is not a kitchen
    /// status. If the order already changed (another board moved it first), nothing is changed and the answer is 409.
    /// </remarks>
    /// <param name="id">Order identifier.</param>
    /// <param name="command">The status to move the order to.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order has the new status.</response>
    /// <response code="400">The status is not InPreparation or Ready.</response>
    /// <response code="404">The order does not exist.</response>
    /// <response code="409">The order already changed, or the status is not its next step.</response>
    [HttpPost]
    [ProducesResponseType<OrderStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderStatusResponse>> HandleAsync(Guid id, ChangeOrderStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var employeeId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(id, command, employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
