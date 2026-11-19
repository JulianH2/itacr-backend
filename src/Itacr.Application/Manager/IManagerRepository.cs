using Itacr.Domain.Orders;

namespace Itacr.Application.Manager;

/// <summary>The two channels the manager compares: the orders a waiter took, and the pickup orders of customers.</summary>
public enum ManagerChannel
{
    Waiter,
    Pickup,
}

/// <summary>The numbers of one channel in a date range, as the database computes them from the orders.</summary>
public sealed record ChannelSummary(
    ManagerChannel Channel,
    int Orders,
    int PreparedOrders,
    double? AveragePreparationSeconds,
    int DeliveredOrders,
    decimal Sales);

/// <summary>An hour of UTC with what happened in it: the orders created and the sales of the orders delivered.</summary>
public sealed record SalesTrendPoint(DateTime HourStart, int Orders, decimal Sales);

/// <summary>A row of the table of recent orders.</summary>
public sealed record ManagerOrder(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    decimal Total,
    DateTime CreatedAt,
    DateTime? ReadyAt,
    DateTime? DeliveredAt,
    int? PreparationSeconds);

/// <summary>One page of orders and how many orders match the filters in all.</summary>
public sealed record ManagerOrdersPage(int TotalCount, IReadOnlyList<ManagerOrder> Orders);

/// <summary>Reads the orders to add them up for the manager's dashboard (HU_16). It only reads; no metric is stored.</summary>
public interface IManagerRepository
{
    /// <summary>The numbers of both channels for the orders created, and delivered, in the range [<paramref name="from"/>, <paramref name="to"/>).</summary>
    Task<IReadOnlyList<ChannelSummary>> GetChannelSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken);

    /// <summary>The hours of the range that had orders or sales, oldest first.</summary>
    Task<IReadOnlyList<SalesTrendPoint>> GetSalesTrendAsync(DateTime from, DateTime to, CancellationToken cancellationToken);

    /// <summary>The orders created in the range, newest first, filtered by channel and status, one page.</summary>
    Task<ManagerOrdersPage> GetOrdersAsync(
        DateTime from,
        DateTime to,
        ManagerChannel? channel,
        OrderStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
