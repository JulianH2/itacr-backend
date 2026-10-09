using Itacr.Domain.Common;

namespace Itacr.Domain.Catalog;

public sealed class Product : AuditableEntity
{
    public Guid CategoryId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsCombo { get; set; }
}
