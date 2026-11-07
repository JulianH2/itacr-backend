using Itacr.Api.Common.Http;
using Itacr.Application.Catalog.GetComboOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Catalog;

/// <summary>The options of a combo are part of the menu, shown to waiters and customers alike, so they are public.</summary>
[ApiController]
[AllowAnonymous]
[Tags("Catalog")]
[Route("api/v1/catalog/combo-options")]
public sealed class GetComboOptionsEndpoint(GetComboOptionsHandler handler) : ControllerBase
{
    /// <summary>Lists the sizes, drinks, extras, and ingredients to leave out that a combo can be customized with.</summary>
    /// <remarks>An order refers to an option by its id and the API takes its price from the menu, never from the request.</remarks>
    /// <response code="200">The options, ordered by type, price, and name.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ComboOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ComboOptionResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
