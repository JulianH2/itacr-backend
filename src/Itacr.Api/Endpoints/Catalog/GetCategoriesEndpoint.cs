using Itacr.Api.Common.Http;
using Itacr.Application.Catalog.GetCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Catalog;

/// <summary>
/// The menu, shown to waiters and customers alike, so it is public. Lists are returned as plain arrays
/// (not paginated): the menu is small and bounded, and the client filters it by category.
/// </summary>
[ApiController]
[AllowAnonymous]
[Tags("Catalog")]
[Route("api/v1/catalog/categories")]
public sealed class GetCategoriesEndpoint(GetCategoriesHandler handler) : ControllerBase
{
    /// <summary>Lists the menu categories in display order.</summary>
    /// <response code="200">The categories, possibly empty.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
