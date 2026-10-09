using FluentValidation;

namespace Itacr.Application.Identity.LoginWithPin;

public sealed class LoginWithPinValidator : AbstractValidator<LoginWithPinCommand>
{
    private const string FourDigits = "^[0-9]{4}$";

    public LoginWithPinValidator()
    {
        RuleFor(command => command.Pin)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(AuthProblems.PinIncomplete)
            .Matches(FourDigits).WithMessage(AuthProblems.PinIncomplete);
    }
}
