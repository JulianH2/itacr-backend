namespace Itacr.Application.Identity;

public enum CredentialVerification
{
    Failed,
    Success,

    /// <summary>The value is right, but the hash uses older settings: store a new hash of the same value.</summary>
    SuccessRehashNeeded,
}

/// <summary>Hashes and verifies PINs and passwords. Never stores or logs the plain value.</summary>
public interface ICredentialHasher
{
    string Hash(string value);

    CredentialVerification Verify(string hash, string value);
}
