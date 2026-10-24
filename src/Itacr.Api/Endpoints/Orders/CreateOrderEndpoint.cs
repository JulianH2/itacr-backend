using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.CreateOrder;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders")]
public sealed class CreateOrderEndpoint(CreateOrderHandler handler) : ControllerBase
{
    /// <summary>Sends an order taken by the waiter to the kitchen.</summary>
    /// <remarks>
    /// Idempotent on <c>clientOrderId</c>: sending the same order again returns the order already saved with 200 instead of
    /// creating a duplicate. Prices come from the menu, never from the request. An order sent offline carries
    /// <c>createdAt</c>, the time it was sent on the device: it can be up to 12 hours old and at most 2 minutes ahead.
    /// </remarks>
    /// <response code="201">The order was saved with status New.</response>
    /// <response code="200">The order had already been saved; it is returned as it is.</response>
    /// <response code="400">Invalid order: no table and not to take away, no products, an unknown table or product, or a createdAt out of range.</response>
    /// <response code="409">The clientOrderId belongs to an order of another waiter.</response>
    [HttpPost]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateOrderResponse>> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(command, waiterId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        var outcome = result.Value;
        return outcome.Created ? StatusCode(StatusCodes.Status201Created, outcome.Order) : Ok(outcome.Order);
    }
}
