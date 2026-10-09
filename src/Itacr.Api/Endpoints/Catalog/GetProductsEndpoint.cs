using Itacr.Api.Common.Http;
using Itacr.Application.Catalog.GetProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Catalog;

/// <summary>The products of one menu category, public like the rest of the menu.</summary>
[ApiController]
[AllowAnonymous]
[Tags("Catalog")]
[Route("api/v1/catalog/products")]
public sealed class GetProductsEndpoint(GetProductsHandler handler) : ControllerBase
{
    /// <summary>Lists the products of one category.</summary>
    /// <param name="categoryId">Category whose products are returned.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The products of the category; an empty array when it has none.</response>
    /// <response code="400">The categoryId query parameter is missing or is not a valid GUID.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> HandleAsync(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetProductsQuery(categoryId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
