namespace Itacr.Application.Notifications;

/// <summary>
/// VAPID keys of Web Push (HU_09), from the "WebPush" section: App Service settings in QA and production, user-secrets locally. The
/// private key is a secret and never leaves the API. Without both keys push is off: subscriptions are still saved and orders keep
/// working, but nothing is sent.
/// </summary>
public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    /// <summary>Contact of the sender for the push services, a <c>mailto:</c> or <c>https:</c> URI.</summary>
    public string Subject { get; init; } = "mailto:itacr@example.com";

    public string? PublicKey { get; init; }

    public string? PrivateKey { get; init; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
