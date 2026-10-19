using FluentValidation;
using Itacr.Application.Catalog;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Customers;
using Itacr.Domain.Orders;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Orders.CreatePickupOrder;

/// <summary><paramref name="Created"/> is false when the request repeated an order that already existed.</summary>
public sealed record CreatePickupOrderOutcome(PickupOrderResponse Order, bool Created);

/// <summary>
/// A customer without an account sends an order to pick up: it is saved as New on the MobileApp channel with menu prices and
/// the customer found (or created) by phone, so it joins the kitchen queue as "Para recoger". Sending the same order again
/// (same ClientOrderId) returns the order already saved instead of a duplicate, and a phone cannot have more active orders
/// than <see cref="PublicOrdersOptions.MaxActiveOrdersPerPhone"/>.
/// </summary>
public sealed class CreatePickupOrderHandler(
    IPickupOrderRepository pickupOrderRepository,
    ICatalogRepository catalogRepository,
    TimeProvider timeProvider,
    IOptions<PublicOrdersOptions> options,
    IValidator<CreatePickupOrderCommand> validator)
{
    public async Task<Result<CreatePickupOrderOutcome>> HandleAsync(CreatePickupOrderCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        if (await pickupOrderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken) is { } existing)
        {
            return new CreatePickupOrderOutcome(PickupOrderResponse.From(existing), Created: false);
        }

        var products = (await catalogRepository.GetProductsByIdsAsync([.. command.Items.Select(item => item.ProductId)], cancellationToken))
            .ToDictionary(product => product.Id);
        if (products.Count != command.Items.Count)
        {
            return OrdersErrors.UnknownProducts;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var order = Order.CreatePickup(command.ClientOrderId, now);
        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, products[item.ProductId].Price, size: null, drink: null, extras: [], exclusions: [], now);
        }

        var customer = new Customer { Name = command.Name!.Trim(), Phone = command.Phone!, CreatedAt = now, UpdatedAt = now };
        var maxActiveOrders = options.Value.MaxActiveOrdersPerPhone;
        switch (await pickupOrderRepository.CreateAsync(order, customer, maxActiveOrders, cancellationToken))
        {
            case PickupCreateOutcome.LimitReached:
                return OrdersErrors.PickupLimitReached(maxActiveOrders);

            case PickupCreateOutcome.AlreadyExists:
                // Another request with the same ClientOrderId was saved first. If it is not a pickup order, the id is taken.
                return await pickupOrderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken) is { } winner
                    ? new CreatePickupOrderOutcome(PickupOrderResponse.From(winner), Created: false)
                    : OrdersErrors.PickupOrderIdTaken;

            default:
                return new CreatePickupOrderOutcome(ToResponse(order, products), Created: true);
        }
    }

    private static PickupOrderResponse ToResponse(Order order, IReadOnlyDictionary<Guid, Domain.Catalog.Product> products) => new(
        order.ClientOrderId,
        order.OrderNumber,
        order.Status,
        order.Total,
        order.CreatedAt,
        [.. order.Items.Select(item => new PickupOrderItemResponse(products[item.ProductId].Name, item.Quantity, item.UnitPrice, item.Subtotal))]);
}
