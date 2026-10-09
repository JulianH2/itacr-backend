using FluentValidation;

namespace Itacr.Application.Identity.GetActiveEmployees;

public sealed class GetActiveEmployeesValidator : AbstractValidator<GetActiveEmployeesQuery>
{
    public GetActiveEmployeesValidator()
    {
        RuleFor(query => query.Role).IsInEnum().WithMessage(AuthProblems.RoleInvalid);
    }
}
