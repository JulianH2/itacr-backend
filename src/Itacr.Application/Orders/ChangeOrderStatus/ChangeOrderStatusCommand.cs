using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.ChangeOrderStatus;

/// <summary>The status the kitchen moves an order to: InPreparation or Ready, the next step of the one it has.</summary>
public sealed record ChangeOrderStatusCommand(OrderStatus Status);
