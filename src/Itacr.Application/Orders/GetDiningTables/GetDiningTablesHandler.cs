using Itacr.Application.Common.Results;

namespace Itacr.Application.Orders.GetDiningTables;

/// <summary>Lists the dining tables in number order, for the waiter to assign an order to one.</summary>
public sealed class GetDiningTablesHandler(IDiningTableRepository diningTableRepository)
{
    public async Task<Result<IReadOnlyList<DiningTableResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var tables = await diningTableRepository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<DiningTableResponse>>.Success([.. tables.Select(table => new DiningTableResponse(table.Id, table.Number))]);
    }
}
