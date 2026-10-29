using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.DeliverOrder;

/// <summary>An order after a waiter handed it over.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="Status">Always Delivered.</param>
/// <param name="DeliveredAt">UTC instant when it was delivered.</param>
public sealed record DeliverOrderResponse(Guid Id, OrderStatus Status, DateTime DeliveredAt);
