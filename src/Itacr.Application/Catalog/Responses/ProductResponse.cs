namespace Itacr.Application.Catalog.Responses;

/// <summary>A menu product.</summary>
/// <param name="Id">Product identifier.</param>
/// <param name="CategoryId">The one category the product belongs to.</param>
/// <param name="Name">Name shown to the user.</param>
/// <param name="Description">Short description; empty when the product has none.</param>
/// <param name="Price">Unit price in MXN with two decimals.</param>
/// <param name="ImageUrl">Image path or URL, or null when the product has no photo.</param>
/// <param name="IsCombo">Whether the product is a combo that can be customized.</param>
public sealed record ProductResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    bool IsCombo);
