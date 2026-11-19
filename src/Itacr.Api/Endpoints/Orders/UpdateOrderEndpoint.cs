using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.GetOrderDetail;
using Itacr.Application.Orders.UpdateOrder;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders/{id:guid}")]
public sealed class UpdateOrderEndpoint(UpdateOrderHandler handler) : ControllerBase
{
    /// <summary>Modifies an order that the kitchen has not finished: its products, quantities, table, and a note.</summary>
    /// <remarks>
    /// Only the waiter who took the order can modify it, and only while it is New or InPreparation. The request replaces every product
    /// of the order; prices come from the menu, never from the request, and the total is recalculated. An order cannot be left without
    /// products. <c>note</c> is added to the notes of the order. The kitchen board shows the order with the "Modificado" mark.
    /// </remarks>
    /// <param name="id">Order identifier.</param>
    /// <param name="command">The new table (for a dine-in order), the products, and an optional note.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The order as it is now, with its new total.</response>
    /// <response code="400">Invalid change: no products, a dine-in order without a table, a table on a take-away order, an unknown table, product, or option, or a note that is too long.</response>
    /// <response code="404">The order does not exist, another waiter took it, or it is a pickup order of a customer.</response>
    /// <response code="409">The order is Ready or Delivered, or its status changed while the request was in progress.</response>
    [HttpPut]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailResponse>> HandleAsync(Guid id, UpdateOrderCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(id, command, waiterId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
