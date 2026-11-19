using System.ComponentModel.DataAnnotations;

namespace Itacr.Application.Identity;

/// <summary>
/// Limit of the public endpoint where a new employee asks for an account (HU_14). It is an option, never a constant in code,
/// so it can be tuned in QA or production without a release.
/// </summary>
public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    /// <summary>Requests one client IP address may send in <see cref="WindowSeconds"/>. Several new employees can share the restaurant's connection.</summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; init; } = 10;

    [Range(1, 86_400)]
    public int WindowSeconds { get; init; } = 3600;
}
