using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Catalog;

namespace Itacr.Application.Catalog.GetProducts;

/// <summary>Lists the products of one category ordered by name; an unknown or empty category returns an empty list.</summary>
public sealed class GetProductsHandler(ICatalogRepository catalogRepository, IValidator<GetProductsQuery> validator)
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } error)
        {
            return error;
        }

        var products = await catalogRepository.GetProductsByCategoryAsync(query.CategoryId!.Value, cancellationToken);
        return Result<IReadOnlyList<ProductResponse>>.Success([.. products.Select(ToResponse)]);
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.CategoryId,
        product.Name,
        product.Description ?? string.Empty,
        product.Price,
        product.ImageUrl,
        product.IsCombo);
}
