using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Endpoints.Catalog;

/// <summary>Query string of <c>GET /api/v1/catalog/products</c>.</summary>
public sealed record GetProductsRequest
{
    /// <summary>Category whose products are returned.</summary>
    [Required(ErrorMessage = "El parámetro categoryId es obligatorio.")]
    public Guid? CategoryId { get; init; }
}
