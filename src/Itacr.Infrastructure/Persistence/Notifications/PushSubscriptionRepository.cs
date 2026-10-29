using Dapper;
using Itacr.Application.Notifications;

namespace Itacr.Infrastructure.Persistence.Notifications;

internal sealed class PushSubscriptionRepository(DbSession session) : IPushSubscriptionRepository
{
    public async Task SaveForEmployeeAsync(Guid employeeId, PushSubscriptionKeys subscription, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(session.Procedure(
            "dbo.usp_PushSubscription_SaveForEmployee",
            new { Id = SequentialGuid.Create(), EmployeeId = employeeId, subscription.Endpoint, subscription.P256dh, subscription.Auth, Now = now },
            cancellationToken));
    }

    public async Task<bool> SaveForPickupOrderAsync(Guid clientOrderId, PushSubscriptionKeys subscription, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<bool>(session.Procedure(
            "dbo.usp_PushSubscription_SaveForPickupOrder",
            new { Id = SequentialGuid.Create(), ClientOrderId = clientOrderId, subscription.Endpoint, subscription.P256dh, subscription.Auth, Now = now },
            cancellationToken));
    }

    public async Task<IReadOnlyList<PushSubscriptionKeys>> GetByRecipientAsync(Guid? employeeId, Guid? customerId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PushSubscriptionKeys>(session.Procedure(
            "dbo.usp_PushSubscription_GetByRecipient",
            new { EmployeeId = employeeId, CustomerId = customerId },
            cancellationToken));
        return [.. rows];
    }

    public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(session.Procedure("dbo.usp_PushSubscription_Delete", new { Endpoint = endpoint }, cancellationToken));
    }
}
