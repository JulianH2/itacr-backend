using System.Threading.Channels;
using Itacr.Application.Notifications;

namespace Itacr.Infrastructure.Notifications;

/// <summary>
/// In-memory queue between the kitchen's request and <see cref="OrderReadyNotificationService"/>. A restart loses what was still
/// queued; that notice is not sent, and the order is still shown as Ready in the app (HU_09 CA4).
/// </summary>
internal sealed class OrderReadyQueue : IOrderReadyQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public void Enqueue(Guid orderId) => _channel.Writer.TryWrite(orderId);
}
