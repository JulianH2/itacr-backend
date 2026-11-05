using Itacr.Application.Common.Results;

namespace Itacr.Application.Catalog.GetComboOptions;

/// <summary>Lists the options a combo can be customized with, so the app shows the same sizes, drinks, extras, and exclusions the API prices.</summary>
public sealed class GetComboOptionsHandler(ICatalogRepository catalogRepository)
{
    public async Task<Result<IReadOnlyList<ComboOptionResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var options = await catalogRepository.GetComboOptionsAsync(cancellationToken);
        return Result<IReadOnlyList<ComboOptionResponse>>.Success(
            [.. options.Select(option => new ComboOptionResponse(option.Id, option.Type, option.Name, option.PriceAdjustment))]);
    }
}
