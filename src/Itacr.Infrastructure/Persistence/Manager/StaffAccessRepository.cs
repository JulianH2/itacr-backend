using Dapper;
using Itacr.Application.Manager;
using Itacr.Domain.Identity;

namespace Itacr.Infrastructure.Persistence.Manager;

internal sealed class StaffAccessRepository(DbSession session) : IStaffAccessRepository
{
    public async Task<PendingAccessRequests> GetPendingAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(session.Procedure("dbo.usp_AccessRequest_GetPending", null, cancellationToken));
        var accounts = await results.ReadAsync<RequestRow>();
        var pinResets = await results.ReadAsync<PinResetRow>();
        return new PendingAccessRequests(
            [.. accounts.Select(row => new PendingAccount(row.Id, row.FullName, Enum.Parse<EmployeeRole>(row.Role), Utc(row.RequestedAt)))],
            [.. pinResets.Select(row => new PendingPinReset(row.EmployeeId, row.FullName, Enum.Parse<EmployeeRole>(row.Role), Utc(row.RequestedAt)))]);
    }

    public async Task<AccessRequestOutcome> ReviewAccountAsync(
        Guid employeeId,
        EmployeeStatus newStatus,
        Guid managerId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var outcome = await connection.QuerySingleAsync<int>(session.Procedure(
            "dbo.usp_Employee_ReviewAccess",
            new { Id = employeeId, NewStatus = newStatus.ToString(), ReviewedById = managerId, Now = now },
            cancellationToken));
        return ToOutcome(outcome);
    }

    public async Task<AccessRequestOutcome> AssignTemporaryPinAsync(Guid employeeId, string pinHash, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var outcome = await connection.QuerySingleAsync<int>(session.Procedure(
            "dbo.usp_Employee_AssignTemporaryPin",
            new { EmployeeId = employeeId, PinHash = pinHash, Now = now },
            cancellationToken));
        return ToOutcome(outcome);
    }

    private static AccessRequestOutcome ToOutcome(int outcome) => outcome switch
    {
        1 => AccessRequestOutcome.Applied,
        2 => AccessRequestOutcome.AlreadyHandled,
        _ => AccessRequestOutcome.NotFound,
    };

    // datetime2 comes back without a kind; every timestamp is stored in UTC.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed record RequestRow(Guid Id, string FullName, string Role, DateTime RequestedAt);

    private sealed record PinResetRow(Guid EmployeeId, string FullName, string Role, DateTime RequestedAt);
}
