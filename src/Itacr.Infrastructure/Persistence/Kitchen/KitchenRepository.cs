using System.Text.Json;
using Dapper;
using Itacr.Application.Kitchen;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Kitchen;

internal sealed class KitchenRepository(DbSession session) : IKitchenRepository
{
    private static readonly JsonSerializerOptions ItemsJson = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ActiveOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActiveOrderRow>(session.Procedure("dbo.usp_Order_GetActive", null, cancellationToken));
        return [.. rows.Select(ToActiveOrder)];
    }

    private static ActiveOrder ToActiveOrder(ActiveOrderRow row) => new(
        row.Id,
        row.OrderNumber,
        Enum.Parse<OrderChannel>(row.Channel),
        Enum.Parse<OrderStatus>(row.Status),
        row.TableNumber,
        // datetime2 comes back without a kind; every timestamp is stored in UTC.
        DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
        row.ItemsJson is null ? [] : JsonSerializer.Deserialize<List<ActiveOrderItem>>(row.ItemsJson, ItemsJson) ?? []);

    private sealed record ActiveOrderRow(
        Guid Id,
        int OrderNumber,
        string Channel,
        string Status,
        int? TableNumber,
        DateTime CreatedAt,
        string? ItemsJson);
}
