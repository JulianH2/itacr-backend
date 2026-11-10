using System.Threading.RateLimiting;
using Itacr.Application.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Itacr.Api.Common;

/// <summary>
/// The limit of the public endpoint where a new employee asks for an account (HU_14): its options, validated at startup, and its
/// rate limit policy. Each client IP address has its own window, on top of the global limit.
/// </summary>
public static class RegistrationServiceCollectionExtensions
{
    public const string RateLimitPolicy = "registration";

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddRegistration(this IServiceCollection services)
    {
        services.AddOptions<RegistrationOptions>().BindConfiguration(RegistrationOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RegistrationOptions>>(AddPolicy);

        return services;
    }

    private static void AddPolicy(RateLimiterOptions limiter, IOptions<RegistrationOptions> settings)
    {
        var options = settings.Value;
        limiter.AddPolicy(RateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0,
                }));
    }
}
