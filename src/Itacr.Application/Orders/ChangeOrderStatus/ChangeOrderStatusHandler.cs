using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.ChangeOrderStatus;

/// <summary>
/// The kitchen taps an order to move it one step: New to InPreparation, then InPreparation to Ready. The change and its time are
/// recorded in the order history. When two boards move the same order at once, the second one gets a conflict instead of
/// moving it twice.
/// </summary>
public sealed class ChangeOrderStatusHandler(
    IOrderRepository orderRepository,
    TimeProvider timeProvider,
    IValidator<ChangeOrderStatusCommand> validator)
{
    public async Task<Result<OrderStatusResponse>> HandleAsync(Guid orderId, ChangeOrderStatusCommand command, Guid employeeId, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        if (await orderRepository.GetStatusAsync(orderId, cancellationToken) is not { } order)
        {
            return OrdersErrors.OrderNotFound;
        }

        if (!OrderStatusRules.CanChange(order.Status, command.Status))
        {
            return order.Status >= command.Status ? OrdersErrors.StatusAlreadyChanged : OrdersErrors.StatusStepNotAllowed;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (!await orderRepository.ChangeStatusAsync(orderId, order.Status, command.Status, employeeId, now, cancellationToken))
        {
            return OrdersErrors.StatusAlreadyChanged;
        }

        return new OrderStatusResponse(orderId, command.Status, now);
    }
}
