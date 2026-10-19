using System.Text.Json;
using Dapper;
using Itacr.Application.Orders;
using Itacr.Domain.Customers;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Orders;

internal sealed class PickupOrderRepository(DbSession session) : IPickupOrderRepository
{
    private static readonly JsonSerializerOptions ItemsJson = new(JsonSerializerDefaults.Web);

    public async Task<PickupOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PickupOrderRow>(
            session.Procedure("dbo.usp_Order_GetPickupByClientOrderId", new { ClientOrderId = clientOrderId }, cancellationToken));
        if (row is null)
        {
            return null;
        }

        return new PickupOrder(
            row.ClientOrderId,
            row.OrderNumber,
            Enum.Parse<OrderStatus>(row.Status),
            row.Total,
            // datetime2 comes back without a kind; every timestamp is stored in UTC.
            DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
            row.ItemsJson is null ? [] : JsonSerializer.Deserialize<List<PickupOrderItem>>(row.ItemsJson, ItemsJson) ?? []);
    }

    public async Task<PickupCreateOutcome> CreateAsync(Order order, Customer customer, int maxActiveOrders, CancellationToken cancellationToken)
    {
        var (itemsJson, historyJson) = OrderPayload.Prepare(order);
        customer.Id = SequentialGuid.Create();

        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var saved = await connection.QuerySingleAsync<SavedPickupRow>(session.Procedure(
            "dbo.usp_Order_CreatePickup",
            new
            {
                order.Id,
                order.ClientOrderId,
                CustomerId = customer.Id,
                customer.Name,
                customer.Phone,
                order.Total,
                Now = order.CreatedAt,
                MaxActiveOrders = maxActiveOrders,
                ItemsJson = itemsJson,
                HistoryJson = historyJson,
            },
            cancellationToken));

        var outcome = Enum.Parse<PickupCreateOutcome>(saved.Outcome);
        if (outcome == PickupCreateOutcome.Created)
        {
            order.OrderNumber = saved.OrderNumber!.Value;
            order.CustomerId = customer.Id;
        }

        return outcome;
    }

    private sealed record PickupOrderRow(Guid ClientOrderId, int OrderNumber, string Status, decimal Total, DateTime CreatedAt, string? ItemsJson);

    private sealed record SavedPickupRow(string Outcome, int? OrderNumber);
}
