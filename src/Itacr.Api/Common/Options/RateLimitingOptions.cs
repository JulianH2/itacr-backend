using System.ComponentModel.DataAnnotations;

namespace Itacr.Api.Common.Options;

/// <summary>Global rate limit applied per client IP address.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10_000)]
    public int PermitLimit { get; init; }

    [Range(1, 3_600)]
    public int WindowSeconds { get; init; }
}
