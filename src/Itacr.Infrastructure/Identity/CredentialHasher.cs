using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Itacr.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity's PBKDF2 hasher with its default options, the same one that produced every stored PIN and
/// password hash (and the migrator's seed), so existing hashes keep verifying and old formats ask for a rehash.
/// </summary>
internal sealed class CredentialHasher(IPasswordHasher<Employee> passwordHasher) : ICredentialHasher
{
    public string Hash(string value) => passwordHasher.HashPassword(Unused, value);

    public CredentialVerification Verify(string hash, string value) => passwordHasher.VerifyHashedPassword(Unused, hash, value) switch
    {
        PasswordVerificationResult.Success => CredentialVerification.Success,
        PasswordVerificationResult.SuccessRehashNeeded => CredentialVerification.SuccessRehashNeeded,
        _ => CredentialVerification.Failed,
    };

    // PasswordHasher<TUser> never reads the user: the hash depends only on its options.
    private static readonly Employee Unused = new() { FullName = string.Empty, Email = string.Empty, Phone = string.Empty, PinHash = string.Empty };
}
