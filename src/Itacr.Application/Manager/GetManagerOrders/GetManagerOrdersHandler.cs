using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;

namespace Itacr.Application.Manager.GetManagerOrders;

/// <summary>The table of recent orders of the manager's dashboard (HU_16), filtered by channel, status, and date range.</summary>
public sealed class GetManagerOrdersHandler(IManagerRepository managerRepository, IValidator<GetManagerOrdersQuery> validator)
{
    public async Task<Result<ManagerOrdersResponse>> HandleAsync(GetManagerOrdersQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } error)
        {
            return error;
        }

        var page = await managerRepository.GetOrdersAsync(
            query.From!.Value.UtcDateTime,
            query.To!.Value.UtcDateTime,
            query.Channel,
            query.Status,
            (query.Page - 1) * query.PageSize,
            query.PageSize,
            cancellationToken);

        return new ManagerOrdersResponse(
            query.Page,
            query.PageSize,
            page.TotalCount,
            [.. page.Orders.Select(order => new ManagerOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Channel,
                order.Status,
                order.TableNumber,
                order.Total,
                order.CreatedAt,
                order.ReadyAt,
                order.DeliveredAt,
                order.PreparationSeconds))]);
    }
}
