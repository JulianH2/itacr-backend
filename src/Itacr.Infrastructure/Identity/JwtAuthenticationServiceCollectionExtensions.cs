using Itacr.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Itacr.Infrastructure.Identity;

internal static class JwtAuthenticationServiceCollectionExtensions
{
    private static readonly TimeSpan TokenClockSkew = TimeSpan.FromMinutes(1);

    /// <summary>Validates the bearer tokens that <see cref="JwtTokenService"/> issues, with the same settings.</summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(ConfigureJwtBearer);

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
}
