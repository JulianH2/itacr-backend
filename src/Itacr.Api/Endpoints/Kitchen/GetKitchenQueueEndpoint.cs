using Itacr.Api.Common.Http;
using Itacr.Application.Kitchen.GetKitchenQueue;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Kitchen;

/// <summary>The kitchen board: every active order in one queue. Kitchen staff and managers can read it.</summary>
[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Kitchen) + "," + nameof(EmployeeRole.Manager))]
[Tags("Kitchen")]
[Route("api/v1/kitchen/queue")]
public sealed class GetKitchenQueueEndpoint(GetKitchenQueueHandler handler) : ControllerBase
{
    /// <summary>Lists the active orders, oldest first.</summary>
    /// <remarks>
    /// Returns the orders in New, InPreparation, and Ready, whatever their channel, ordered by creation time. The board polls it
    /// every few seconds, so it is never cached. <c>serverTime</c> lets the client measure elapsed time without trusting its own clock.
    /// </remarks>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The active orders; <c>orders</c> is empty when there are none.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not kitchen staff or a manager.</response>
    [HttpGet]
    [ProducesResponseType<KitchenQueueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KitchenQueueResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
