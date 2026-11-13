using System.Net;
using System.Security.Cryptography;
using Itacr.Application.Notifications;
using Itacr.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Api.Tests.Notifications;

/// <summary>
/// The sender of Web Push (ITACR-66): it signs the notice with the VAPID keys and encrypts it for the browser. The push service is
/// replaced with a stub, so the tests check what the API answers when the service accepts the notice, when it says the subscription is
/// gone, and when it fails.
/// </summary>
public sealed class WebPushSenderTests
{
    private const string Endpoint = "https://push.example.com/subscription/itacr-1";

    private static readonly PushMessage Message = new("Pedido #1042", "La orden #1042 está lista", "/orders/1042", Guid.NewGuid());

    [Fact]
    public async Task SendAsync_ShouldNotSendAnythingAndShouldFail_WhenTheVapidKeysAreNotConfigured()
    {
        // Arrange
        var handler = new StubPushHandler(HttpStatusCode.Created);
        var sender = CreateSender(handler, configured: false);

        // Act
        var delivery = await sender.SendAsync(Subscription(), Message, CancellationToken.None);

        // Assert
        Assert.Equal(PushDelivery.Failed, delivery);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SendAsync_ShouldSendTheNotice_WhenThePushServiceAcceptsIt()
    {
        // Arrange
        var handler = new StubPushHandler(HttpStatusCode.Created);
        var sender = CreateSender(handler);

        // Act
        var delivery = await sender.SendAsync(Subscription(), Message, CancellationToken.None);

        // Assert
        Assert.Equal(PushDelivery.Sent, delivery);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Endpoint, request.RequestUri?.AbsoluteUri);
        Assert.NotEmpty(request.Body);
    }

    [Fact]
    public async Task SendAsync_ShouldReportThatTheSubscriptionIsGone_WhenThePushServiceAnswersNotFound()
    {
        // Arrange
        var sender = CreateSender(new StubPushHandler(HttpStatusCode.NotFound));

        // Act
        var delivery = await sender.SendAsync(Subscription(), Message, CancellationToken.None);

        // Assert
        Assert.Equal(PushDelivery.Gone, delivery);
    }

    [Fact]
    public async Task SendAsync_ShouldReportThatTheSubscriptionIsGone_WhenThePushServiceAnswersGone()
    {
        // Arrange
        var sender = CreateSender(new StubPushHandler(HttpStatusCode.Gone));

        // Act
        var delivery = await sender.SendAsync(Subscription(), Message, CancellationToken.None);

        // Assert
        Assert.Equal(PushDelivery.Gone, delivery);
    }

    [Fact]
    public async Task SendAsync_ShouldFailWithoutThrowing_WhenThePushServiceRejectsTheNotice()
    {
        // Arrange
        var sender = CreateSender(new StubPushHandler(HttpStatusCode.InternalServerError));

        // Act
        var delivery = await sender.SendAsync(Subscription(), Message, CancellationToken.None);

        // Assert
        Assert.Equal(PushDelivery.Failed, delivery);
    }

    private static PushSubscriptionKeys Subscription()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(false);
        var publicKey = Base64Url([0x04, .. parameters.Q.X!, .. parameters.Q.Y!]);
        return new PushSubscriptionKeys(Endpoint, publicKey, Base64Url(RandomNumberGenerator.GetBytes(16)));
    }

    private static IPushSender CreateSender(StubPushHandler handler, bool configured = true)
    {
        var settings = new Dictionary<string, string?> { ["WebPush:Subject"] = "mailto:itacr@example.com" };
        if (configured)
        {
            var (publicKey, privateKey) = VapidKeys();
            settings["WebPush:PublicKey"] = publicKey;
            settings["WebPush:PrivateKey"] = privateKey;
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddLogging();
        services.AddPushNotifications();
        services.AddHttpClient("IPushSender").ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider().GetRequiredService<IPushSender>();
    }

    /// <summary>The VAPID key pair (RFC 8292): the public key is the point of the curve and the private one is the scalar.</summary>
    private static (string PublicKey, string PrivateKey) VapidKeys()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);
        return (Base64Url([0x04, .. parameters.Q.X!, .. parameters.Q.Y!]), Base64Url(parameters.D!));
    }

    /// <summary>Base64 without the padding and with the characters of the URL (RFC 4648, section 5).</summary>
    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>The push service, replaced by a stub that answers the same status every time.</summary>
    private sealed class StubPushHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri? RequestUri, byte[] Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri, request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken)));
            return new HttpResponseMessage(statusCode);
        }
    }
}
