namespace Itacr.Application.Catalog;

/// <summary>Row returned by <c>dbo.fn_GetProductsByCategory</c>; property names match the function's columns.</summary>
internal sealed class ProductRow
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsCombo { get; set; }
}
