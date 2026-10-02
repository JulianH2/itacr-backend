using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(64)]
    public string Secret { get; init; } = string.Empty;

    [Range(1, 24)]
    public int TokenLifetimeHours { get; init; } = 12;
}
