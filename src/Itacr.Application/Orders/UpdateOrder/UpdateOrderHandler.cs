using FluentValidation;
using Itacr.Application.Catalog;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Application.Orders.GetOrderDetail;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.UpdateOrder;

/// <summary>
/// A waiter modifies an order they took while the kitchen has not finished it (HU_13): products, quantities, table, and a note.
/// Prices come from the menu again, so the total is recalculated. A pickup order of a customer and the order of another waiter
/// answer "not found", like in the detail; a Ready or Delivered order answers a conflict, and so does an order whose status
/// changed between the read and the write. An order is never left without products.
/// </summary>
public sealed class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    ICatalogRepository catalogRepository,
    TimeProvider timeProvider,
    IValidator<UpdateOrderCommand> validator)
{
    public async Task<Result<OrderDetailResponse>> HandleAsync(Guid orderId, UpdateOrderCommand command, Guid waiterId, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        if (command.Items.Count > OrderLimits.MaxItems)
        {
            return OrdersErrors.ItemsLimitExceeded;
        }

        if (await orderRepository.GetDetailAsync(orderId, cancellationToken) is not { } order || order.WaiterId != waiterId)
        {
            return OrdersErrors.OrderNotFound;
        }

        if (!OrderStatusRules.CanModify(order.Status))
        {
            return OrdersErrors.OrderNotModifiable;
        }

        if (order.Channel == OrderChannel.DineIn ? command.TableId is null : command.TableId is not null)
        {
            return order.Channel == OrderChannel.DineIn ? OrdersErrors.TableRequired : OrdersErrors.TakeAwayHasNoTable;
        }

        if (command.TableId is { } tableId
            && (await diningTableRepository.GetAllAsync(cancellationToken)).All(table => table.Id != tableId))
        {
            return OrdersErrors.UnknownTable;
        }

        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = (await catalogRepository.GetProductsByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        if (products.Count != productIds.Count)
        {
            return OrdersErrors.UnknownProducts;
        }

        var comboOptions = await OrderItemCustomization.LoadOptionsAsync(catalogRepository, command.Items, cancellationToken);
        var lines = new List<ModifiedOrderItem>(command.Items.Count);
        for (var index = 0; index < command.Items.Count; index++)
        {
            var item = command.Items[index];
            var resolved = OrderItemCustomization.Resolve(products[item.ProductId], item, index, comboOptions);
            if (!resolved.IsSuccess)
            {
                return resolved.Error!;
            }

            var line = resolved.Value;
            lines.Add(new ModifiedOrderItem(item.ProductId, item.Quantity, line.UnitPrice, line.Size, line.Drink, line.Extras, line.Exclusions));
        }

        var note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        var modification = new OrderModification(orderId, waiterId, command.TableId, lines, note, timeProvider.GetUtcNow().UtcDateTime);
        if (!await orderRepository.UpdateAsync(modification, cancellationToken))
        {
            return OrdersErrors.OrderChangedMeanwhile;
        }

        var updated = await orderRepository.GetDetailAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} was updated but was not found.");
        return OrderDetailResponse.From(updated);
    }
}
