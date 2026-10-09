using Dapper;
using Itacr.Application.Orders;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Orders;

internal sealed class DiningTableRepository(DbSession session) : IDiningTableRepository
{
    public async Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var tables = await connection.QueryAsync<DiningTable>(session.Procedure("dbo.usp_DiningTable_GetAll", null, cancellationToken));
        return [.. tables];
    }
}
