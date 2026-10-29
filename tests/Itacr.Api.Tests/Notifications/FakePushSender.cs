using System.Collections.Concurrent;
using Itacr.Application.Notifications;

namespace Itacr.Api.Tests.Notifications;

/// <summary>Records the pushes the API sends instead of calling a push service. The notices are sent in the background, so tests wait for them.</summary>
public sealed class FakePushSender : IPushSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentQueue<(PushSubscriptionKeys Subscription, PushMessage Message)> _sent = new();

    /// <summary>Endpoints the push service answers as expired.</summary>
    public ConcurrentDictionary<string, bool> GoneEndpoints { get; } = new();

    public IReadOnlyList<(PushSubscriptionKeys Subscription, PushMessage Message)> Sent => [.. _sent];

    public Task<PushDelivery> SendAsync(PushSubscriptionKeys subscription, PushMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue((subscription, message));
        return Task.FromResult(GoneEndpoints.ContainsKey(subscription.Endpoint) ? PushDelivery.Gone : PushDelivery.Sent);
    }

    public async Task<IReadOnlyList<(PushSubscriptionKeys Subscription, PushMessage Message)>> WaitForAsync(int count)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (_sent.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        return Sent;
    }

    /// <summary>Gives the background service time to send anything it still has; for tests that expect nothing more to arrive.</summary>
    public static Task SettleAsync() => Task.Delay(TimeSpan.FromMilliseconds(500));
}
