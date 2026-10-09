using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Itacr.Api.Tests.Auth;

/// <summary>The hasher keeps the format of every stored PIN and password hash, and says when one needs a rehash.</summary>
public sealed class CredentialHasherTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const byte IdentityV3FormatMarker = 0x01;

    private static readonly PasswordHasher<Employee> StoredFormat = new();

    private static readonly PasswordHasher<Employee> OldFormat = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2,
    }));

    private ICredentialHasher Hasher => factory.Services.GetRequiredService<ICredentialHasher>();

    [Fact]
    public void Verify_ShouldSucceed_WhenTheHashWasStoredBeforeITACR38()
    {
        // Arrange: the same hasher and options as the old AuthService and the migrator's seed.
        var hash = StoredFormat.HashPassword(null!, "1111");

        // Act
        var verification = Hasher.Verify(hash, "1111");

        // Assert
        Assert.Equal(CredentialVerification.Success, verification);
    }

    [Fact]
    public void Verify_ShouldFail_WhenTheValueIsWrong()
    {
        // Arrange
        var hash = StoredFormat.HashPassword(null!, "1111");

        // Act
        var verification = Hasher.Verify(hash, "2222");

        // Assert
        Assert.Equal(CredentialVerification.Failed, verification);
    }

    [Fact]
    public void Verify_ShouldAskForARehash_WhenTheHashUsesAnOlderFormat()
    {
        // Arrange
        var hash = OldFormat.HashPassword(null!, "1111");

        // Act
        var verification = Hasher.Verify(hash, "1111");

        // Assert
        Assert.Equal(CredentialVerification.SuccessRehashNeeded, verification);
    }

    [Fact]
    public void Hash_ShouldProduceTheStoredFormat_WhenAValueIsHashed()
    {
        // Act
        var hash = Hasher.Hash("1111");

        // Assert
        Assert.Equal(IdentityV3FormatMarker, Convert.FromBase64String(hash)[0]);
        Assert.Equal(PasswordVerificationResult.Success, StoredFormat.VerifyHashedPassword(null!, hash, "1111"));
    }
}
