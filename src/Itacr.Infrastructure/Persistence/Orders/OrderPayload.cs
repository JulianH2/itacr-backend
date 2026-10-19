using System.Text.Json;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Orders;

/// <summary>
/// The items and the status history of a new order as the JSON the stored procedures read. It gives every row its key and its
/// <c>OrderId</c> (the domain never sets them), so every procedure that saves an order saves it the same way.
/// </summary>
internal static class OrderPayload
{
    /// <summary>Gives the order and its children their keys, and returns the JSON of the items and of the history.</summary>
    public static (string ItemsJson, string HistoryJson) Prepare(Order order)
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

        return (JsonSerializer.Serialize(items), JsonSerializer.Serialize(history));
    }
}
