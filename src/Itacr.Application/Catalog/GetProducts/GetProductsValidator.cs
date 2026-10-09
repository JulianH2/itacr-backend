using FluentValidation;

namespace Itacr.Application.Catalog.GetProducts;

public sealed class GetProductsValidator : AbstractValidator<GetProductsQuery>
{
    public const string CategoryIdRequired = "El parámetro categoryId es obligatorio.";

    public GetProductsValidator()
    {
        RuleFor(query => query.CategoryId).NotNull().WithMessage(CategoryIdRequired);
    }
}
