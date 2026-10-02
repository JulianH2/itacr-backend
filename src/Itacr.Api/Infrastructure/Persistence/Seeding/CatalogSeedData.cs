using Itacr.Api.Features.Catalog;
using Itacr.Api.Features.Orders;

namespace Itacr.Api.Infrastructure.Persistence.Seeding;

/// <summary>
/// Reference data needed in every environment, loaded with HasData. Ids and timestamps are fixed so every
/// environment generates the same migration and never duplicates a row.
/// </summary>
internal static class CatalogSeedData
{
    private static readonly DateTime SeedTimestamp = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Category[] Categories =
    [
        NewCategory(1, "Combos"),
        NewCategory(2, "Hamburguesas"),
        NewCategory(3, "Complementos"),
        NewCategory(4, "Bebidas"),
        NewCategory(5, "Postres"),
    ];

    public static readonly Product[] Products =
    [
        NewProduct(1, 1, "Combo Clásico", "Hamburguesa clásica, papas a la francesa y refresco.", 119m, "combo-clasico", isCombo: true),
        NewProduct(2, 1, "Combo Doble", "Hamburguesa doble carne, papas a la francesa y refresco.", 149m, "combo-doble", isCombo: true),
        NewProduct(3, 1, "Combo Pollo", "Hamburguesa de pollo crujiente, papas a la francesa y refresco.", 129m, "combo-pollo", isCombo: true),
        NewProduct(4, 1, "Combo Infantil", "Mini hamburguesa, papas a la francesa y jugo.", 89m, "combo-infantil", isCombo: true),
        NewProduct(5, 2, "Hamburguesa Clásica", "Carne de res, lechuga, jitomate y cebolla.", 79m, "hamburguesa-clasica"),
        NewProduct(6, 2, "Hamburguesa con Queso", "Carne de res, queso americano y aderezo de la casa.", 89m, "hamburguesa-con-queso"),
        NewProduct(7, 2, "Hamburguesa Doble", "Doble carne de res con doble queso.", 109m, "hamburguesa-doble"),
        NewProduct(8, 2, "Hamburguesa de Pollo", "Pechuga de pollo empanizada con lechuga y mayonesa.", 95m, "hamburguesa-de-pollo"),
        NewProduct(9, 3, "Papas a la Francesa", "Porción de papas fritas con sal.", 39m, "papas-a-la-francesa"),
        NewProduct(10, 3, "Aros de Cebolla", "Aros de cebolla empanizados y crujientes.", 45m, "aros-de-cebolla"),
        NewProduct(11, 3, "Nuggets de Pollo", "Seis piezas de nuggets con salsa a elegir.", 55m, "nuggets-de-pollo"),
        NewProduct(12, 4, "Refresco de Cola", "Refresco de cola de 355 ml.", 29m, "refresco-de-cola"),
        NewProduct(13, 4, "Refresco de Lima-Limón", "Refresco de lima-limón de 355 ml.", 29m, "refresco-de-lima-limon"),
        NewProduct(14, 4, "Agua Natural", "Botella de agua natural de 600 ml.", 20m, "agua-natural"),
        NewProduct(15, 4, "Limonada", "Limonada natural preparada al momento.", 32m, "limonada"),
        NewProduct(16, 5, "Helado de Vainilla", "Copa de helado de vainilla.", 35m, "helado-de-vainilla"),
        NewProduct(17, 5, "Pay de Manzana", "Pay caliente de manzana con canela.", 38m, "pay-de-manzana"),
        NewProduct(18, 5, "Brownie con Helado", "Brownie de chocolate con una bola de helado.", 49m, "brownie-con-helado"),
    ];

    public static readonly ComboOption[] ComboOptions =
    [
        NewOption(1, ComboOptionType.Size, "Chico", 0m),
        NewOption(2, ComboOptionType.Size, "Mediano", 15m),
        NewOption(3, ComboOptionType.Size, "Grande", 25m),
        NewOption(4, ComboOptionType.Drink, "Refresco de cola", 0m),
        NewOption(5, ComboOptionType.Drink, "Refresco de lima-limón", 0m),
        NewOption(6, ComboOptionType.Drink, "Agua natural", 0m),
        NewOption(7, ComboOptionType.Extra, "Queso extra", 12m),
        NewOption(8, ComboOptionType.Extra, "Tocino", 18m),
        NewOption(9, ComboOptionType.Extra, "Jalapeños", 8m),
    ];

    public static readonly DiningTable[] DiningTables =
    [
        .. Enumerable.Range(1, 10).Select(number => new DiningTable
        {
            Id = SeedId(4, number),
            Number = number,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp,
        }),
    ];

    private static Guid SeedId(int kind, int number) => Guid.Parse($"{kind}0000000-0000-0000-0000-{number:D12}");

    private static Category NewCategory(int number, string name) => new()
    {
        Id = SeedId(1, number),
        Name = name,
        SortOrder = number,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };

    private static Product NewProduct(int number, int categoryNumber, string name, string description, decimal price, string imageSlug, bool isCombo = false) => new()
    {
        Id = SeedId(2, number),
        CategoryId = SeedId(1, categoryNumber),
        Name = name,
        Description = description,
        Price = price,
        ImageUrl = $"/assets/images/products/{imageSlug}.png",
        IsCombo = isCombo,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };

    private static ComboOption NewOption(int number, ComboOptionType type, string name, decimal priceAdjustment) => new()
    {
        Id = SeedId(3, number),
        Type = type,
        Name = name,
        PriceAdjustment = priceAdjustment,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };
}
