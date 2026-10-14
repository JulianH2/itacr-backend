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

    public async Task<OrderStatusState?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrderStatusRow>(
            session.Procedure("dbo.usp_Order_GetStatus", new { Id = orderId }, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lastChange = new OrderStatusChange(ParseStatus(row.LastFromStatus), Enum.Parse<OrderStatus>(row.LastToStatus), row.LastChangedById, row.LastChangedAt);
        return new OrderStatusState(row.Id, Enum.Parse<OrderStatus>(row.Status), lastChange);
    }

    public async Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, Guid changedById, DateTime now, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<bool>(session.Procedure(
            "dbo.usp_Order_ChangeStatus",
            new
            {
                Id = orderId,
                FromStatus = from.ToString(),
                ToStatus = to.ToString(),
                ChangedById = changedById,
                HistoryId = SequentialGuid.Create(),
                Now = now,
            },
            cancellationToken));
    }

    public async Task<OrderDetail?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrderDetailRow>(
            session.Procedure("dbo.usp_Order_GetById", new { Id = orderId }, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var items = row.ItemsJson is null ? [] : JsonSerializer.Deserialize<List<OrderDetailItem>>(row.ItemsJson, ItemsJson) ?? [];
        var history = row.HistoryJson is null ? [] : JsonSerializer.Deserialize<List<HistoryEntryJson>>(row.HistoryJson, ItemsJson) ?? [];
        return new OrderDetail(
            row.Id,
            row.OrderNumber,
            Enum.Parse<OrderChannel>(row.Channel),
            Enum.Parse<OrderStatus>(row.Status),
            row.TableNumber,
            row.Total,
            row.CreatedAt,
            row.LastStatusChangeAt,
            row.ReadyAt,
            items,
            [.. history.Select(entry => new OrderStatusChange(ParseStatus(entry.FromStatus), Enum.Parse<OrderStatus>(entry.ToStatus), entry.ChangedById, entry.ChangedAt))]);
    }

    public async Task<IReadOnlyList<RecentOrder>> GetRecentByWaiterAsync(Guid waiterId, DateTime since, int take, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<RecentOrderRow>(
            session.Procedure("dbo.usp_Order_GetRecentByWaiter", new { WaiterId = waiterId, Since = since, Take = take }, cancellationToken));
        return [.. rows.Select(row => new RecentOrder(
            row.Id,
            row.OrderNumber,
            Enum.Parse<OrderChannel>(row.Channel),
            Enum.Parse<OrderStatus>(row.Status),
            row.TableNumber,
            row.CreatedAt))];
    }

    private static OrderStatus? ParseStatus(string? status) => status is null ? null : Enum.Parse<OrderStatus>(status);

    private sealed record OrderStatusRow(Guid Id, string Status, string? LastFromStatus, string LastToStatus, Guid? LastChangedById, DateTime LastChangedAt);

    private sealed record OrderDetailRow(
        Guid Id,
        int OrderNumber,
        string Channel,
        string Status,
        int? TableNumber,
        decimal Total,
        DateTime CreatedAt,
        DateTime LastStatusChangeAt,
        DateTime? ReadyAt,
        string? ItemsJson,
        string? HistoryJson);

    private sealed record HistoryEntryJson(string? FromStatus, string ToStatus, Guid? ChangedById, DateTime ChangedAt);

    private sealed record RecentOrderRow(Guid Id, int OrderNumber, string Channel, string Status, int? TableNumber, DateTime CreatedAt);

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
