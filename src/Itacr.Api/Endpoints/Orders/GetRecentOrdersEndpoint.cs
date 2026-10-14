using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Orders.GetRecentOrders;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/orders/recent")]
public sealed class GetRecentOrdersEndpoint(GetRecentOrdersHandler handler) : ControllerBase
{
    /// <summary>Lists the last orders the signed-in waiter sent in the last 12 hours, newest first (at most 10).</summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The orders; empty when the waiter sent none.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecentOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RecentOrderResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var waiterId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(waiterId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
