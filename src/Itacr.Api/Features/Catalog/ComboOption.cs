using Itacr.Api.Common;

namespace Itacr.Api.Features.Catalog;

public sealed class ComboOption : AuditableEntity
{
    public ComboOptionType Type { get; set; }

    public required string Name { get; set; }

    public decimal PriceAdjustment { get; set; }
}
