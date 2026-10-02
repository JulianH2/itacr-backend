using Itacr.Api.Features.Catalog.Requests;
using Itacr.Api.Features.Catalog.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Features.Catalog;

/// <summary>
/// The menu, shown to waiters and customers alike, so it is public. Lists are returned as plain arrays
/// (not paginated): the menu is small and bounded, and the client filters it by category.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/catalog")]
public sealed class CatalogController(ICatalogService catalogService) : ControllerBase
{
    /// <summary>Lists the menu categories in display order.</summary>
    /// <response code="200">The categories, possibly empty.</response>
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetCategoriesAsync(cancellationToken));
    }

    /// <summary>Lists the products of one category.</summary>
    /// <response code="200">The products of the category; an empty array when it has none.</response>
    /// <response code="400">The categoryId query parameter is missing or is not a valid GUID.</response>
    [HttpGet("products")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetProductsAsync(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken)
    {
        // [Required] guarantees a value once [ApiController] has accepted the request.
        return Ok(await catalogService.GetProductsAsync(request.CategoryId!.Value, cancellationToken));
    }
}
