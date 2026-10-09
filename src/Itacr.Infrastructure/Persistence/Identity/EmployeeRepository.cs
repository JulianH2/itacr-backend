using Dapper;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;

namespace Itacr.Infrastructure.Persistence.Identity;

internal sealed class EmployeeRepository(DbSession session) : IEmployeeRepository
{
    public async Task<IReadOnlyList<EmployeeSummary>> GetActiveByRoleAsync(EmployeeRole role, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var employees = await connection.QueryAsync<EmployeeSummary>(
            session.Procedure("dbo.usp_Employee_GetActiveByRole", new { Role = role.ToString() }, cancellationToken));
        return [.. employees];
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Employee>(
            session.Procedure("dbo.usp_Employee_GetById", new { Id = id }, cancellationToken));
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Employee>(
            session.Procedure("dbo.usp_Employee_GetByEmail", new { Email = email }, cancellationToken));
    }

    public async Task<FailedPinLogin> RegisterFailedPinLoginAsync(
        Guid id,
        int maxFailedAttempts,
        DateTime lockoutEndAt,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<FailedPinLogin>(session.Procedure(
            "dbo.usp_Employee_RegisterFailedPinLogin",
            new { Id = id, MaxFailedAttempts = maxFailedAttempts, LockoutEndAt = lockoutEndAt, Now = now },
            cancellationToken));
    }

    public async Task RegisterSuccessfulPinLoginAsync(Guid id, string? newPinHash, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(session.Procedure(
            "dbo.usp_Employee_RegisterSuccessfulPinLogin",
            new { Id = id, NewPinHash = newPinHash, Now = now },
            cancellationToken));
    }

    public async Task<IReadOnlyList<Guid>> GetActiveManagerIdsAsync(Guid excludedEmployeeId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var ids = await connection.QueryAsync<Guid>(
            session.Procedure("dbo.usp_Employee_GetActiveManagerIds", new { ExcludedEmployeeId = excludedEmployeeId }, cancellationToken));
        return [.. ids];
    }
}
