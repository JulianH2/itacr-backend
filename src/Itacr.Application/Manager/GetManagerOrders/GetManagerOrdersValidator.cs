using FluentValidation;

namespace Itacr.Application.Manager.GetManagerOrders;

public sealed class GetManagerOrdersValidator : AbstractValidator<GetManagerOrdersQuery>
{
    public GetManagerOrdersValidator()
    {
        DateRangeRules.Apply(this, query => query.From, query => query.To);

        RuleFor(query => query.Page)
            .InclusiveBetween(1, GetManagerOrdersQuery.MaxPage)
            .WithMessage(ManagerMessages.PageInvalid);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetManagerOrdersQuery.MaxPageSize)
            .WithMessage(ManagerMessages.PageSizeInvalid);
    }
}
