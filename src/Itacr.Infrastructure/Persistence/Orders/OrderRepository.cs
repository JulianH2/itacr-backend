using System.Text.Json;
using Dapper;
using Itacr.Application.Orders;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Orders;

internal sealed class OrderRepository(DbSession session) : IOrderRepository
{
    private static readonly JsonSerializerOptions ItemsJson = new(JsonSerializerDefaults.Web);

    public async Task<PlacedOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PlacedOrderRow>(
            session.Procedure("dbo.usp_Order_GetByClientOrderId", new { ClientOrderId = clientOrderId }, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var items = row.ItemsJson is null ? [] : JsonSerializer.Deserialize<List<PlacedOrderItem>>(row.ItemsJson, ItemsJson) ?? [];
        return new PlacedOrder(
            row.Id,
            row.ClientOrderId,
            row.OrderNumber,
            Enum.Parse<OrderChannel>(row.Channel),
            Enum.Parse<OrderStatus>(row.Status),
            row.WaiterId,
            row.TableNumber,
            row.Total,
            row.CreatedAt,
            items);
    }

    public async Task<bool> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        order.Id = SequentialGuid.Create();
        var items = order.Items.Select(item =>
        {
            item.Id = SequentialGuid.Create();
            item.OrderId = order.Id;
            return new
            {
                item.Id,
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.Subtotal,
                item.Size,
                item.Drink,
                item.Extras,
                item.Exclusions,
            };
        }).ToList();
        var history = order.StatusHistory.Select(entry =>
        {
            entry.Id = SequentialGuid.Create();
            entry.OrderId = order.Id;
            return new
            {
                entry.Id,
                FromStatus = entry.FromStatus?.ToString(),
                ToStatus = entry.ToStatus.ToString(),
                entry.ChangedById,
                entry.CreatedAt,
            };
        }).ToList();

        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var saved = await connection.QuerySingleAsync<SavedOrderRow>(session.Procedure(
            "dbo.usp_Order_Create",
            new
            {
                order.Id,
                order.ClientOrderId,
                Channel = order.Channel.ToString(),
                Status = order.Status.ToString(),
                order.TableId,
                order.WaiterId,
                order.CustomerId,
                order.Total,
                Now = order.CreatedAt,
                ItemsJson = JsonSerializer.Serialize(items),
                HistoryJson = JsonSerializer.Serialize(history),
            },
            cancellationToken));

        if (!saved.Created)
        {
            return false;
        }

        order.OrderNumber = saved.OrderNumber!.Value;
        return true;
    }

    private sealed record PlacedOrderRow(
        Guid Id,
        Guid ClientOrderId,
        int OrderNumber,
        string Channel,
        string Status,
        Guid? WaiterId,
        int? TableNumber,
        decimal Total,
        DateTime CreatedAt,
        string? ItemsJson);

    private sealed record SavedOrderRow(bool Created, int? OrderNumber);
}
