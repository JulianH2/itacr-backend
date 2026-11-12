using Itacr.Domain.Orders;

namespace Itacr.Application.Manager.GetManagerOrders;

/// <summary>
/// The table of recent orders: the orders created in the range (<paramref name="From"/> inclusive, <paramref name="To"/> exclusive, UTC),
/// optionally of one channel and one status, newest first, one page at a time.
/// </summary>
public sealed record GetManagerOrdersQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    ManagerChannel? Channel,
    OrderStatus? Status,
    int Page = 1,
    int PageSize = GetManagerOrdersQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
