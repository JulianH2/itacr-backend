using Itacr.Infrastructure.Identity;

namespace Itacr.Api.Tests.Auth;

public sealed class JwtOptionsTests
{
    [Fact]
    public void JwtOptions_ShouldKeepEveryValue_WhenTheyAreSet()
    {
        // Act
        var options = new JwtOptions
        {
            Issuer = "itacr-api",
            Audience = "itacr-pwa",
            Secret = new string('s', 64),
            TokenLifetimeHours = 8,
        };

        // Assert
        Assert.Equal(("itacr-api", "itacr-pwa", 64, 8), (options.Issuer, options.Audience, options.Secret.Length, options.TokenLifetimeHours));
    }

    [Fact]
    public void JwtOptions_ShouldBeEmptyWithA12HourToken_WhenNothingIsSet()
    {
        // Act
        var options = new JwtOptions();

        // Assert
        Assert.Equal((string.Empty, string.Empty, string.Empty, 12), (options.Issuer, options.Audience, options.Secret, options.TokenLifetimeHours));
    }
}
