using System.Text.RegularExpressions;
using FluentValidation;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity.RegisterEmployee;

public sealed partial class RegisterEmployeeValidator : AbstractValidator<RegisterEmployeeCommand>
{
    private const int NameMinLength = 2;
    private const int NameMaxLength = 120;
    private const int PhoneLength = 10;

    public RegisterEmployeeValidator()
    {
        RuleFor(command => command.FullName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage(AuthProblems.RequiredField)
            .Must(name => name!.Trim().Length is >= NameMinLength and <= NameMaxLength).WithMessage(AuthProblems.FullNameLength)
            .Must(name => !name!.Any(char.IsControl)).WithMessage(AuthProblems.FullNameInvalid);

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .Must(email => !string.IsNullOrWhiteSpace(email)).WithMessage(AuthProblems.RequiredField)
            .Must(email => EmailOrUser().IsMatch(email!.Trim())).WithMessage(AuthProblems.EmailOrUserInvalid);

        RuleFor(command => command.Phone)
            .Cascade(CascadeMode.Stop)
            .Must(phone => !string.IsNullOrWhiteSpace(phone)).WithMessage(AuthProblems.RequiredField)
            .Must(phone => phone!.Trim().Length == PhoneLength && phone.Trim().All(char.IsAsciiDigit)).WithMessage(AuthProblems.PhoneLength);

        // The manager role is never requested from a public form: it is created by the seed or by another manager.
        RuleFor(command => command.Role)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(AuthProblems.RequiredField)
            .Must(role => role is EmployeeRole.Waiter or EmployeeRole.Kitchen).WithMessage(AuthProblems.RoleNotRequestable);

        RuleFor(command => command.Pin)
            .Cascade(CascadeMode.Stop)
            .Must(pin => !string.IsNullOrEmpty(pin)).WithMessage(AuthProblems.RequiredField)
            .Must(pin => FourDigits().IsMatch(pin!)).WithMessage(AuthProblems.PinFourDigits);

        RuleFor(command => command.PinConfirmation)
            .Cascade(CascadeMode.Stop)
            .Must(confirmation => !string.IsNullOrEmpty(confirmation)).WithMessage(AuthProblems.RequiredField)
            .Must((command, confirmation) => confirmation == command.Pin).WithMessage(AuthProblems.PinsDoNotMatch);

        RuleFor(command => command.AcceptedPrivacyNotice)
            .Must(accepted => accepted).WithMessage(AuthProblems.PrivacyNoticeRequired);
    }

    /// <summary>An email or a user name: no spaces and between 3 and 254 characters.</summary>
    [GeneratedRegex(@"^\S{3,254}$")]
    private static partial Regex EmailOrUser();

    [GeneratedRegex("^[0-9]{4}$")]
    private static partial Regex FourDigits();
}
