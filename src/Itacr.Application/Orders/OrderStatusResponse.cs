using Itacr.Domain.Orders;

namespace Itacr.Application.Orders;

/// <summary>The status of an order after the kitchen moved it, or undid that move.</summary>
public sealed record OrderStatusResponse(Guid Id, OrderStatus Status, DateTime LastStatusChangeAt);
