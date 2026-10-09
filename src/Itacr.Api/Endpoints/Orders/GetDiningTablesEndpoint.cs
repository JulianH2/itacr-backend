using Itacr.Api.Common.Http;
using Itacr.Application.Orders.GetDiningTables;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Orders;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Waiter))]
[Tags("Orders")]
[Route("api/v1/dining-tables")]
public sealed class GetDiningTablesEndpoint(GetDiningTablesHandler handler) : ControllerBase
{
    /// <summary>Lists the dining tables in number order, for the waiter to assign an order to one.</summary>
    /// <response code="200">The tables.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DiningTableResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DiningTableResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
