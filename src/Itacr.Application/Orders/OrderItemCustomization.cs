using Itacr.Application.Catalog;
using Itacr.Application.Common.Results;
using Itacr.Application.Orders.CreateOrder;
using Itacr.Domain.Catalog;

namespace Itacr.Application.Orders;

/// <summary>What an order line is saved with once its options are checked: the unit price and the names the kitchen reads.</summary>
internal sealed record CustomizedItem(decimal UnitPrice, string? Size, string? Drink, IReadOnlyList<string> Extras, IReadOnlyList<string> Exclusions);

/// <summary>
/// The rules of a customized combo (HU_12), shared by the waiter order and the customer pickup order. The unit price is the menu
/// price plus the size adjustment plus the price of each extra; the drink and the exclusions do not change it. The subtotal is the
/// unit price times the quantity, and <c>Order.AddItem</c> computes it.
/// </summary>
internal static class OrderItemCustomization
{
    /// <summary>The options the items refer to, by id. The catalog is read only when some item chose an option.</summary>
    public static async Task<IReadOnlyDictionary<Guid, ComboOption>> LoadOptionsAsync(
        ICatalogRepository catalogRepository,
        IEnumerable<CreateOrderItem> items,
        CancellationToken cancellationToken)
    {
        if (!items.Any(item => item.SizeId is not null || item.DrinkId is not null || item.ExtraIds?.Count > 0 || item.ExclusionIds?.Count > 0))
        {
            return new Dictionary<Guid, ComboOption>();
        }

        return (await catalogRepository.GetComboOptionsAsync(cancellationToken)).ToDictionary(option => option.Id);
    }

    /// <summary>Checks the options of one line against its product and prices it. <paramref name="index"/> tells the client which line to fix.</summary>
    public static Result<CustomizedItem> Resolve(Product product, CreateOrderItem item, int index, IReadOnlyDictionary<Guid, ComboOption> options)
    {
        var extraIds = item.ExtraIds ?? [];
        var exclusionIds = item.ExclusionIds ?? [];

        if (!product.IsCombo)
        {
            return item.SizeId is null && item.DrinkId is null && extraIds.Count == 0 && exclusionIds.Count == 0
                ? new CustomizedItem(product.Price, null, null, [], [])
                : Fail(index, OrdersErrors.NotCustomizableMessage);
        }

        if (item.SizeId is null)
        {
            return Fail(index, OrdersErrors.SizeRequiredMessage);
        }

        if (item.DrinkId is null)
        {
            return Fail(index, OrdersErrors.DrinkRequiredMessage);
        }

        if (extraIds.Distinct().Count() != extraIds.Count || exclusionIds.Distinct().Count() != exclusionIds.Count)
        {
            return Fail(index, OrdersErrors.DuplicatedOptionMessage);
        }

        if (!TryGet(options, item.SizeId.Value, ComboOptionType.Size, out var size)
            || !TryGet(options, item.DrinkId.Value, ComboOptionType.Drink, out var drink))
        {
            return Fail(index, OrdersErrors.UnknownOptionMessage);
        }

        var extras = new List<ComboOption>(extraIds.Count);
        foreach (var extraId in extraIds)
        {
            if (!TryGet(options, extraId, ComboOptionType.Extra, out var extra))
            {
                return Fail(index, OrdersErrors.UnknownOptionMessage);
            }

            extras.Add(extra);
        }

        var exclusions = new List<ComboOption>(exclusionIds.Count);
        foreach (var exclusionId in exclusionIds)
        {
            if (!TryGet(options, exclusionId, ComboOptionType.Exclusion, out var exclusion))
            {
                return Fail(index, OrdersErrors.UnknownOptionMessage);
            }

            exclusions.Add(exclusion);
        }

        var unitPrice = product.Price + size.PriceAdjustment + extras.Sum(extra => extra.PriceAdjustment);
        return new CustomizedItem(unitPrice, size.Name, drink.Name, [.. extras.Select(extra => extra.Name)], [.. exclusions.Select(exclusion => exclusion.Name)]);
    }

    private static bool TryGet(IReadOnlyDictionary<Guid, ComboOption> options, Guid id, ComboOptionType type, out ComboOption option) =>
        options.TryGetValue(id, out option!) && option.Type == type;

    private static Error Fail(int index, string message) =>
        Error.Validation(new Dictionary<string, string[]> { [$"items[{index}]"] = [message] });
}
