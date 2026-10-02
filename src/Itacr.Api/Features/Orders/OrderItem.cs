using Itacr.Api.Common;

namespace Itacr.Api.Features.Orders;

public sealed class OrderItem : AuditableEntity
{
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? Size { get; set; }

    public string? Drink { get; set; }

    public List<string> Extras { get; set; } = [];

    public List<string> Exclusions { get; set; } = [];
}
