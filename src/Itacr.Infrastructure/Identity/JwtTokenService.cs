using System.Text;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Itacr.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public LoginResponse CreateLogin(Employee employee)
    {
        var settings = options.Value;
        var issuedAt = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = issuedAt.AddHours(settings.TokenLifetimeHours);
        var displayName = EmployeeDisplayName.From(employee.FullName);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [AuthClaimTypes.Subject] = employee.Id.ToString(),
                [AuthClaimTypes.Name] = displayName,
                [AuthClaimTypes.Role] = employee.Role.ToString(),
            },
            SigningCredentials = new SigningCredentials(CreateSigningKey(settings.Secret), SecurityAlgorithms.HmacSha256),
        };

        return new LoginResponse(
            TokenHandler.CreateToken(descriptor),
            expiresAt,
            new SessionEmployeeResponse(employee.Id, displayName, employee.Role));
    }

    public static SymmetricSecurityKey CreateSigningKey(string secret) => new(Encoding.UTF8.GetBytes(secret));
}
