using FluentValidation;

namespace Itacr.Application.Orders.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    private const int MaxItems = 50;
    private const int MinQuantity = 1;
    private const int MaxQuantity = 99;

    public CreateOrderValidator()
    {
        RuleFor(command => command.ClientOrderId).NotEmpty().WithMessage(OrdersErrors.ClientOrderIdRequired);

        RuleFor(command => command)
            .Must(command => command.TableId is not null || command.TakeAway)
            .WithMessage(OrdersErrors.DestinationRequired)
            .OverridePropertyName(nameof(CreateOrderCommand.TableId));

        RuleFor(command => command)
            .Must(command => command.TableId is null || !command.TakeAway)
            .WithMessage(OrdersErrors.DestinationAmbiguous)
            .OverridePropertyName(nameof(CreateOrderCommand.TableId));

        RuleFor(command => command.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(OrdersErrors.ItemsRequired)
            .Must(items => items.Count <= MaxItems).WithMessage(OrdersErrors.TooManyItems)
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count).WithMessage(OrdersErrors.DuplicatedProduct);

        // Reported per item ("items[0]"), so the client knows which line to fix.
        RuleForEach(command => command.Items)
            .Must(item => item.Quantity is >= MinQuantity and <= MaxQuantity)
            .WithMessage(OrdersErrors.QuantityOutOfRange);
    }
}
