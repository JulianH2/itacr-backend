using FluentValidation;

namespace Itacr.Application.Manager.AssignTemporaryPin;

public sealed class AssignTemporaryPinValidator : AbstractValidator<AssignTemporaryPinCommand>
{
    private const string FourDigits = "^[0-9]{4}$";

    public AssignTemporaryPinValidator()
    {
        RuleFor(command => command.Pin)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ManagerMessages.PinRequired)
            .Matches(FourDigits).WithMessage(ManagerMessages.PinFourDigits);
    }
}
