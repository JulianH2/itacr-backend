using System.Threading.RateLimiting;
using Itacr.Application.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Itacr.Api.Common;

/// <summary>The HTTP side of the sign-in: authorization and the stricter rate limit of the login endpoints. Bearer tokens are configured by Infrastructure.</summary>
public static class AuthServiceCollectionExtensions
{
    public const string LoginRateLimitPolicy = "login";

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddOptions<LoginSecurityOptions>().BindConfiguration(LoginSecurityOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddAuthorization();
        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<LoginSecurityOptions>>(AddLoginRateLimit);

        return services;
    }

    private static void AddLoginRateLimit(RateLimiterOptions limiter, IOptions<LoginSecurityOptions> settings)
    {
        var loginSecurity = settings.Value;
        limiter.AddPolicy(LoginRateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginSecurity.PermitLimit,
                    Window = TimeSpan.FromSeconds(loginSecurity.WindowSeconds),
                    QueueLimit = 0,
                }));
    }
}
