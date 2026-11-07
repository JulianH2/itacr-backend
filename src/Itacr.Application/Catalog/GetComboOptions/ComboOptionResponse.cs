using Itacr.Domain.Catalog;

namespace Itacr.Application.Catalog.GetComboOptions;

/// <summary>An option a combo can be customized with.</summary>
/// <param name="Id">Option identifier; an order refers to the option by it.</param>
/// <param name="Type">Size, Drink, Extra, or Exclusion (an ingredient that can be left out).</param>
/// <param name="Name">Name shown to the user and written on the kitchen card, e.g. "Grande" or "Cebolla".</param>
/// <param name="PriceAdjustment">What the option adds to the unit price in MXN: a size or an extra costs, a drink or an exclusion does not.</param>
public sealed record ComboOptionResponse(Guid Id, ComboOptionType Type, string Name, decimal PriceAdjustment);
