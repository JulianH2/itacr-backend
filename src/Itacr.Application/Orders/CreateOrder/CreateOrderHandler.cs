using FluentValidation;
using Itacr.Application.Catalog;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Catalog;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.CreateOrder;

/// <summary>
/// A waiter sends an order to the kitchen: it is saved as New with menu prices, for a table or to take away. Sending
/// the same order again (same ClientOrderId) returns the order already saved instead of a duplicate. An order that
/// waited offline keeps the time it was sent on the device, so the kitchen sees it in its original place.
/// </summary>
public sealed class CreateOrderHandler(
    IOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    ICatalogRepository catalogRepository,
    TimeProvider timeProvider,
    IValidator<CreateOrderCommand> validator)
{
    private static readonly TimeSpan MaxClockAhead = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MaxOfflineAge = TimeSpan.FromHours(12);

    public async Task<Result<CreateOrderOutcome>> HandleAsync(CreateOrderCommand command, Guid waiterId, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        if (await orderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken) is { } existing)
        {
            return Replay(existing, waiterId);
        }

        var now = timeProvider.GetUtcNow();
        var createdAt = command.CreatedAt ?? now;
        if (createdAt > now + MaxClockAhead)
        {
            return OrdersErrors.CreatedAtInTheFuture;
        }

        if (createdAt < now - MaxOfflineAge)
        {
            return OrdersErrors.CreatedAtTooOld;
        }

        int? tableNumber = null;
        if (command.TableId is { } tableId)
        {
            var tables = await diningTableRepository.GetAllAsync(cancellationToken);
            if (tables.FirstOrDefault(table => table.Id == tableId) is not { } table)
            {
                return OrdersErrors.UnknownTable;
            }

            tableNumber = table.Number;
        }

        var products = (await catalogRepository.GetProductsByIdsAsync([.. command.Items.Select(item => item.ProductId)], cancellationToken))
            .ToDictionary(product => product.Id);
        if (products.Count != command.Items.Count)
        {
            return OrdersErrors.UnknownProducts;
        }

        var channel = command.TakeAway ? OrderChannel.TakeAway : OrderChannel.DineIn;
        var order = Order.Create(command.ClientOrderId, channel, command.TableId, waiterId, customerId: null, createdAt.UtcDateTime);
        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, products[item.ProductId].Price, size: null, drink: null, extras: [], exclusions: [], createdAt.UtcDateTime);
        }

        if (!await orderRepository.CreateAsync(order, cancellationToken))
        {
            // Another request with the same ClientOrderId was saved first.
            var winner = await orderRepository.GetByClientOrderIdAsync(command.ClientOrderId, cancellationToken)
                ?? throw new InvalidOperationException($"Order {command.ClientOrderId} was reported as existing but was not found.");
            return Replay(winner, waiterId);
        }

        return new CreateOrderOutcome(ToResponse(order, tableNumber, products), Created: true);
    }

    private static Result<CreateOrderOutcome> Replay(PlacedOrder existing, Guid waiterId)
    {
        if (existing.WaiterId != waiterId)
        {
            return OrdersErrors.PlacedByAnotherWaiter;
        }

        var response = new CreateOrderResponse(
            existing.Id,
            existing.ClientOrderId,
            existing.OrderNumber,
            existing.Status,
            existing.Channel,
            existing.TableNumber,
            existing.Total,
            existing.CreatedAt,
            [.. existing.Items.Select(item => new CreateOrderItemResponse(item.ProductId, item.Name, item.Quantity, item.UnitPrice, item.Subtotal))]);
        return new CreateOrderOutcome(response, Created: false);
    }

    private static CreateOrderResponse ToResponse(Order order, int? tableNumber, IReadOnlyDictionary<Guid, Product> products) => new(
        order.Id,
        order.ClientOrderId,
        order.OrderNumber,
        order.Status,
        order.Channel,
        tableNumber,
        order.Total,
        order.CreatedAt,
        [.. order.Items.Select(item => new CreateOrderItemResponse(item.ProductId, products[item.ProductId].Name, item.Quantity, item.UnitPrice, item.Subtotal))]);
}
