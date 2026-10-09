using Itacr.Domain.Common;

namespace Itacr.Domain.Catalog;

public sealed class ComboOption : AuditableEntity
{
    public ComboOptionType Type { get; set; }

    public required string Name { get; set; }

    public decimal PriceAdjustment { get; set; }
}
