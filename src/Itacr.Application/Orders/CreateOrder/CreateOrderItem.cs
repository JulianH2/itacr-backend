namespace Itacr.Application.Orders.CreateOrder;

/// <summary>
/// A line of an order: a product, how many, and, for a combo, the options chosen from <c>GET /catalog/combo-options</c> (HU_12).
/// The size and the drink are required for a combo; extras and exclusions are optional. Other products take no options. The
/// options are ids, so the price always comes from the menu and the request never carries one.
/// </summary>
public sealed record CreateOrderItem(
    Guid ProductId,
    int Quantity,
    Guid? SizeId = null,
    Guid? DrinkId = null,
    IReadOnlyList<Guid>? ExtraIds = null,
    IReadOnlyList<Guid>? ExclusionIds = null)
{
    /// <summary>
    /// Identifies the line: the same product with the same options is the same line, whatever the order of its extras and
    /// exclusions. The same combo with other options is another line.
    /// </summary>
    public string LineKey() => string.Join('|', ProductId, SizeId, DrinkId, Sorted(ExtraIds), Sorted(ExclusionIds));

    private static string Sorted(IReadOnlyList<Guid>? ids) => string.Join(',', (ids ?? []).Order());
}
