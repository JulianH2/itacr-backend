using Itacr.Application.Common.Results;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.UndoOrderStatusChange;

/// <summary>
/// Undoes an accidental tap on the kitchen board: the order goes back to the status it had before its last kitchen step, if that
/// step was taken by the same employee a few seconds ago. The undo is recorded in the history too; nothing is deleted.
/// </summary>
public sealed class UndoOrderStatusChangeHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
{
    public async Task<Result<OrderStatusResponse>> HandleAsync(Guid orderId, Guid employeeId, CancellationToken cancellationToken)
    {
        if (await orderRepository.GetStatusAsync(orderId, cancellationToken) is not { } order)
        {
            return OrdersErrors.OrderNotFound;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var last = order.LastChange;
        if (last.ToStatus != order.Status || !OrderStatusRules.CanUndo(last.FromStatus, last.ToStatus, last.ChangedAt, now))
        {
            return OrdersErrors.NothingToUndo;
        }

        if (last.ChangedById != employeeId)
        {
            return OrdersErrors.UndoByAnotherEmployee;
        }

        var previous = last.FromStatus!.Value;
        if (!await orderRepository.ChangeStatusAsync(orderId, order.Status, previous, employeeId, now, cancellationToken))
        {
            return OrdersErrors.NothingToUndo;
        }

        return new OrderStatusResponse(orderId, previous, now);
    }
}
