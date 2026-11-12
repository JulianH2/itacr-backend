using Itacr.Domain.Orders;

namespace Itacr.Application.Manager.GetManagerOrders;

/// <summary>A row of the table of recent orders.</summary>
/// <param name="Id">Order identifier; the detail of the order is <c>GET /orders/{id}</c>.</param>
/// <param name="OrderNumber">Number shown to staff and customers.</param>
/// <param name="Channel">DineIn or TakeAway (a waiter took it) or MobileApp (a customer sent it).</param>
/// <param name="Status">New, InPreparation, Ready, or Delivered.</param>
/// <param name="TableNumber">Table of a dine-in order, null otherwise.</param>
/// <param name="Total">Total of the order in MXN.</param>
/// <param name="CreatedAt">UTC instant when the order was created.</param>
/// <param name="ReadyAt">UTC instant when the order became Ready; null until then.</param>
/// <param name="DeliveredAt">UTC instant when the order was delivered; null until then.</param>
/// <param name="PreparationSeconds">Seconds between the creation and the moment it became Ready; null while it is not Ready.</param>
public sealed record ManagerOrderResponse(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    decimal Total,
    DateTime CreatedAt,
    DateTime? ReadyAt,
    DateTime? DeliveredAt,
    int? PreparationSeconds);

/// <summary>One page of the table of recent orders.</summary>
/// <param name="Page">Page number, from 1.</param>
/// <param name="PageSize">Rows per page.</param>
/// <param name="TotalCount">Orders that match the filters, in all the pages.</param>
/// <param name="Items">The rows of this page, newest first.</param>
public sealed record ManagerOrdersResponse(int Page, int PageSize, int TotalCount, IReadOnlyList<ManagerOrderResponse> Items);
