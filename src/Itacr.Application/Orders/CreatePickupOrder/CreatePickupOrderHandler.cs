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

        if (command.Items.Count > options.Value.MaxProducts)
        {
            return OrdersErrors.ItemsLimitExceeded;
        }

        if (await pickupOrderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken) is { } existing)
        {
            return new CreatePickupOrderOutcome(PickupOrderResponse.From(existing), Created: false);
        }

        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = (await catalogRepository.GetProductsByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        if (products.Count != productIds.Count)
        {
            return OrdersErrors.UnknownProducts;
        }

        var customized = new List<CustomizedItem>(command.Items.Count);
        var comboOptions = await OrderItemCustomization.LoadOptionsAsync(catalogRepository, command.Items, cancellationToken);
        for (var index = 0; index < command.Items.Count; index++)
        {
            var resolved = OrderItemCustomization.Resolve(products[command.Items[index].ProductId], command.Items[index], index, comboOptions);
            if (!resolved.IsSuccess)
            {
                return resolved.Error!;
            }

            customized.Add(resolved.Value);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var order = Order.CreatePickup(command.ClientOrderId, now);
        for (var index = 0; index < command.Items.Count; index++)
        {
            var item = command.Items[index];
            var line = customized[index];
            order.AddItem(item.ProductId, item.Quantity, line.UnitPrice, line.Size, line.Drink, line.Extras, line.Exclusions, now);
        }

        var customer = new Customer { Name = command.Name!.Trim(), Phone = command.Phone!, CreatedAt = now, UpdatedAt = now };
        var maxActiveOrders = options.Value.MaxActiveOrdersPerPhone;
        return await pickupOrderRepository.CreateAsync(order, customer, maxActiveOrders, cancellationToken) switch
        {
            PickupCreateOutcome.LimitReached => OrdersErrors.PickupLimitReached(maxActiveOrders),

            // Another request with the same ClientOrderId was saved first. If it is not a pickup order, the id is taken.
            PickupCreateOutcome.AlreadyExists => await pickupOrderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken) is { } winner
                ? new CreatePickupOrderOutcome(PickupOrderResponse.From(winner), Created: false)
                : OrdersErrors.PickupOrderIdTaken,

            _ => new CreatePickupOrderOutcome(ToResponse(order, products), Created: true),
        };
    }

    private static PickupOrderResponse ToResponse(Order order, Dictionary<Guid, Domain.Catalog.Product> products) => new(
        order.ClientOrderId,
        order.OrderNumber,
        order.Status,
        order.Total,
        order.CreatedAt,
        [.. order.Items.Select(item => new PickupOrderItemResponse(products[item.ProductId].Name, item.Quantity, item.UnitPrice, item.Subtotal))]);
}
