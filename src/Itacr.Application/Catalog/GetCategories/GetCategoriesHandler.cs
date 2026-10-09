using Itacr.Application.Common.Results;

namespace Itacr.Application.Catalog.GetCategories;

/// <summary>Lists the menu categories in display order.</summary>
public sealed class GetCategoriesHandler(ICatalogRepository catalogRepository)
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var categories = await catalogRepository.GetCategoriesAsync(cancellationToken);
        return Result<IReadOnlyList<CategoryResponse>>.Success(
            [.. categories.Select(category => new CategoryResponse(category.Id, category.Name, category.SortOrder))]);
    }
}
