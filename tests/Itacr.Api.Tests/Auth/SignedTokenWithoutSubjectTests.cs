using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Itacr.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Itacr.Api.Tests.Auth;

/// <summary>
/// A token with a valid signature, issuer, audience, and role but no employee id (<c>sub</c>) cannot act as anyone: the endpoints
/// that need the employee answer 401 instead of working with an empty identity.
/// </summary>
public sealed class SignedTokenWithoutSubjectTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string AnyId = "00000000-0000-0000-0000-000000000001";

    [Theory]
    [InlineData("GET", "/api/v1/auth/me", EmployeeRole.Waiter)]
    [InlineData("GET", "/api/v1/orders/active", EmployeeRole.Waiter)]
    [InlineData("GET", "/api/v1/orders/recent", EmployeeRole.Waiter)]
    [InlineData("GET", $"/api/v1/orders/{AnyId}", EmployeeRole.Waiter)]
    [InlineData("POST", "/api/v1/orders", EmployeeRole.Waiter)]
    [InlineData("PUT", $"/api/v1/orders/{AnyId}", EmployeeRole.Waiter)]
    [InlineData("POST", $"/api/v1/orders/{AnyId}/deliver", EmployeeRole.Waiter)]
    [InlineData("POST", "/api/v1/push/subscriptions", EmployeeRole.Waiter)]
    [InlineData("POST", $"/api/v1/orders/{AnyId}/status", EmployeeRole.Kitchen)]
    [InlineData("POST", $"/api/v1/orders/{AnyId}/status/undo", EmployeeRole.Kitchen)]
    [InlineData("POST", $"/api/v1/manager/access-requests/{AnyId}/approve", EmployeeRole.Manager)]
    [InlineData("POST", $"/api/v1/manager/access-requests/{AnyId}/reject", EmployeeRole.Manager)]
    public async Task Send_ShouldAnswerUnauthorized_WhenTheTokenIsSignedButHasNoEmployeeId(string method, string url, EmployeeRole role)
    {
        // Arrange
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject(role));
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string TokenWithoutSubject(EmployeeRole role)
    {
        var settings = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                [AuthClaimTypes.Name] = "Sin Identificador",
                [AuthClaimTypes.Role] = role.ToString(),
            },
            SigningCredentials = new SigningCredentials(JwtTokenService.CreateSigningKey(ApiFactory.TestJwtSecret), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
