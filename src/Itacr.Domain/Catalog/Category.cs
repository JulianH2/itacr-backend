using Itacr.Domain.Common;

namespace Itacr.Domain.Catalog;

public sealed class Category : AuditableEntity
{
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Product> Products { get; } = [];
}
