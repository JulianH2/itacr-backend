using FluentValidation;

namespace Itacr.Application.Orders.UpdateOrder;

public sealed class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{
    public const int MaxNoteLength = 500;

    private const int MaxItems = 50;
    private const int MinQuantity = 1;
    private const int MaxQuantity = 99;

    public UpdateOrderValidator()
    {
        RuleFor(command => command.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(OrdersErrors.OrderCannotBeEmpty)
            .Must(items => items.Count <= MaxItems).WithMessage(OrdersErrors.TooManyItems)
            .Must(items => items.Select(item => item.LineKey()).Distinct().Count() == items.Count).WithMessage(OrdersErrors.DuplicatedProduct);

        RuleForEach(command => command.Items)
            .Must(item => item.Quantity is >= MinQuantity and <= MaxQuantity)
            .WithMessage(OrdersErrors.QuantityOutOfRange);

        RuleFor(command => command.Note)
            .Must(note => note is null || note.Trim().Length <= MaxNoteLength)
            .WithMessage(OrdersErrors.NoteTooLong);
    }
}
