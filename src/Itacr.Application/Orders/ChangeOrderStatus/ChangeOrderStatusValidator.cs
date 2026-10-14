using FluentValidation;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.ChangeOrderStatus;

public sealed class ChangeOrderStatusValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    public ChangeOrderStatusValidator()
    {
        RuleFor(command => command.Status)
            .Must(OrderStatusRules.IsKitchenStep)
            .WithMessage(OrdersErrors.KitchenStatusOnly);
    }
}
