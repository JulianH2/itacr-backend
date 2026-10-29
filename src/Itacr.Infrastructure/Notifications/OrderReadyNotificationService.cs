using Itacr.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Itacr.Infrastructure.Notifications;

/// <summary>Sends the "order ready" notices one order at a time, each in its own scope, so one failure never stops the next.</summary>
internal sealed class OrderReadyNotificationService(
    OrderReadyQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderReadyNotificationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var orderId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OrderReadyNotifier>().NotifyAsync(orderId, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The ready notice of order {OrderId} could not be sent", orderId);
            }
        }
    }
}
