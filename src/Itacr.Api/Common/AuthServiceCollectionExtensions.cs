using System.Threading.RateLimiting;
using Itacr.Application.Identity;
using Itacr.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Itacr.Api.Common;

public static class AuthServiceCollectionExtensions
{
    public const string LoginRateLimitPolicy = "login";

    private const string UnknownClient = "unknown";
    private static readonly TimeSpan TokenClockSkew = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddOptions<LoginSecurityOptions>().BindConfiguration(LoginSecurityOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(ConfigureJwtBearer);
        services.AddAuthorization();

        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<LoginSecurityOptions>>(AddLoginRateLimit);

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions bearer, IOptions<JwtOptions> settings)
    {
        var jwt = settings.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.Secret),
            NameClaimType = AuthClaimTypes.Name,
            RoleClaimType = AuthClaimTypes.Role,
            ClockSkew = TokenClockSkew,
        };
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
