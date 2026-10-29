using Dapper;
using Itacr.Application.Notifications;
using Itacr.Domain.Notifications;

namespace Itacr.Infrastructure.Persistence.Notifications;

internal sealed class NotificationRepository(DbSession session) : INotificationRepository
{
    public async Task CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        notification.Id = SequentialGuid.Create();
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(session.Procedure(
            "dbo.usp_Notification_Create",
            new
            {
                notification.Id,
                Type = notification.Type.ToString(),
                notification.RecipientEmployeeId,
                notification.SubjectEmployeeId,
                notification.OrderId,
                notification.SentAt,
                Now = notification.CreatedAt,
            },
            cancellationToken));
    }

    public async Task<OrderReadyTarget?> CreateOrderReadyAsync(Guid orderId, DateTime now, CancellationToken cancellationToken)
    {
        var notificationId = SequentialGuid.Create();
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrderReadyRow>(session.Procedure(
            "dbo.usp_Notification_CreateOrderReady",
            new { Id = notificationId, OrderId = orderId, Now = now },
            cancellationToken));
        return row is null
            ? null
            : new OrderReadyTarget(notificationId, row.OrderNumber, row.Channel, row.TableNumber, row.WaiterId, row.CustomerId);
    }

    public async Task MarkReadAsync(Guid notificationId, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(session.Procedure("dbo.usp_Notification_MarkRead", new { Id = notificationId, Now = now }, cancellationToken));
    }

    private sealed record OrderReadyRow(int OrderNumber, string Channel, int? TableNumber, Guid? WaiterId, Guid? CustomerId);
}
