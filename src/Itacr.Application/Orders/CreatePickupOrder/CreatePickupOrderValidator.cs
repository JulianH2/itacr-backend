using FluentValidation;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Orders.CreatePickupOrder;

public sealed class CreatePickupOrderValidator : AbstractValidator<CreatePickupOrderCommand>
{
    private const int NameMinLength = 2;
    private const int NameMaxLength = 60;
    private const int PhoneLength = 10;
    private const string SequentialPhone = "1234567890";

    public CreatePickupOrderValidator(IOptions<PublicOrdersOptions> options)
    {
        var limits = options.Value;

        RuleFor(command => command.ClientOrderId).NotEmpty().WithMessage(OrdersErrors.ClientOrderIdRequired);

        RuleFor(command => command.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage(OrdersErrors.PickupFieldRequired)
            .Must(name => name!.Trim().Length is >= NameMinLength and <= NameMaxLength).WithMessage(OrdersErrors.PickupNameLength)
            .Must(name => !name!.Any(char.IsControl)).WithMessage(OrdersErrors.PickupNameInvalid);

        RuleFor(command => command.Phone)
            .Cascade(CascadeMode.Stop)
            .Must(phone => !string.IsNullOrWhiteSpace(phone)).WithMessage(OrdersErrors.PickupFieldRequired)
            .Must(phone => phone!.Length == PhoneLength && phone.All(char.IsAsciiDigit)).WithMessage(OrdersErrors.PickupPhoneLength)
            .Must(phone => !IsPlaceholder(phone!)).WithMessage(OrdersErrors.PickupPhoneInvalid);

        RuleFor(command => command.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(OrdersErrors.ItemsRequired)
            .Must(items => items.Count <= limits.MaxProducts)
                .WithMessage($"Un pedido para recoger admite hasta {limits.MaxProducts} productos.")
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count).WithMessage(OrdersErrors.DuplicatedProduct);

        // Reported per item ("items[0]"), so the client knows which line to fix.
        RuleForEach(command => command.Items)
            .Must(item => item.Quantity >= 1 && item.Quantity <= limits.MaxQuantity)
            .WithMessage($"Cada producto admite de 1 a {limits.MaxQuantity} unidades.");
    }

    /// <summary>0000000000 and the other repeated digits, and 1234567890: numbers typed to get past the form.</summary>
    private static bool IsPlaceholder(string phone) => phone.Distinct().Count() == 1 || phone == SequentialPhone;
}
