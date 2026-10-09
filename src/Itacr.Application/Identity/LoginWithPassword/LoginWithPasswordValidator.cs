using FluentValidation;

namespace Itacr.Application.Identity.LoginWithPassword;

public sealed class LoginWithPasswordValidator : AbstractValidator<LoginWithPasswordCommand>
{
    private const int EmailMaxLength = 254;
    private const int PasswordMaxLength = 128;

    public LoginWithPasswordValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(AuthProblems.EmailRequired)
            .MaximumLength(EmailMaxLength).WithMessage(AuthProblems.EmailInvalid)
            .EmailAddress().WithMessage(AuthProblems.EmailInvalid);

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(AuthProblems.PasswordRequired)
            .MaximumLength(PasswordMaxLength).WithMessage(AuthProblems.InvalidPassword);
    }
}
