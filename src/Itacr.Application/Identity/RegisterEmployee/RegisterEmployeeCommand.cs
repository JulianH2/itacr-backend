using Itacr.Domain.Identity;

namespace Itacr.Application.Identity.RegisterEmployee;

/// <summary>
/// A new employee asks for an account (HU_14). <paramref name="Email"/> is the email or the user name: one value that is unique and
/// that the password login looks up. The role can only be Waiter or Kitchen, and the PIN is asked twice.
/// </summary>
public sealed record RegisterEmployeeCommand(
    string? FullName,
    string? Email,
    string? Phone,
    EmployeeRole? Role,
    string? Pin,
    string? PinConfirmation,
    bool AcceptedPrivacyNotice);
