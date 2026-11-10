using FluentValidation;

namespace Itacr.Application.Manager.GetManagerDashboard;

public sealed class GetManagerDashboardValidator : AbstractValidator<GetManagerDashboardQuery>
{
    public GetManagerDashboardValidator()
    {
        DateRangeRules.Apply(this, query => query.From, query => query.To);
    }
}
