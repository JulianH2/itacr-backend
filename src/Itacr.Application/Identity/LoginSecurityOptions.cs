using System.ComponentModel.DataAnnotations;

namespace Itacr.Application.Identity;

public sealed class LoginSecurityOptions
{
    public const string SectionName = "LoginSecurity";

    [Range(1, 20)]
    public int MaxFailedPinAttempts { get; init; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; init; } = 15;

    [Range(1, 10000)]
    public int PermitLimit { get; init; } = 10;

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;
}
