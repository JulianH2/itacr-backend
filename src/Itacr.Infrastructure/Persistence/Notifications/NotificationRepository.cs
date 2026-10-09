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
}
