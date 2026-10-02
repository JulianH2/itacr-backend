namespace Itacr.Api.Features.Catalog.Responses;

/// <summary>A menu category.</summary>
/// <param name="Id">Category identifier.</param>
/// <param name="Name">Name shown to the user, e.g. "Combos".</param>
/// <param name="SortOrder">Ascending display order.</param>
public sealed record CategoryResponse(Guid Id, string Name, int SortOrder);
