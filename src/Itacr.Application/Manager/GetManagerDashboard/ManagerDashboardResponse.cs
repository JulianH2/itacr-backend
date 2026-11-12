namespace Itacr.Application.Manager.GetManagerDashboard;

/// <summary>The numbers of one channel in the range.</summary>
/// <param name="Channel">Waiter (the orders a waiter took, for a table or to take away) or Pickup (the orders of customers).</param>
/// <param name="Orders">Orders created in the range.</param>
/// <param name="DeliveredOrders">Orders delivered in the range.</param>
/// <param name="Sales">Total of the orders delivered in the range, in MXN.</param>
/// <param name="PreparedOrders">Orders created in the range that reached Ready; the average is over them.</param>
/// <param name="AveragePreparationSeconds">Average time between the creation of an order and the moment it became Ready, in seconds; null when no order was prepared.</param>
public sealed record ChannelMetrics(
    ManagerChannel Channel,
    int Orders,
    int DeliveredOrders,
    decimal Sales,
    int PreparedOrders,
    int? AveragePreparationSeconds);

/// <summary>An hour of the range that had orders or sales.</summary>
/// <param name="HourStart">Start of the hour, as a UTC instant.</param>
/// <param name="Orders">Orders created in that hour.</param>
/// <param name="Sales">Total of the orders delivered in that hour, in MXN.</param>
public sealed record TrendPoint(DateTime HourStart, int Orders, decimal Sales);

/// <summary>The summary of the manager's dashboard for a date range.</summary>
/// <param name="From">Start of the range, inclusive (UTC).</param>
/// <param name="To">End of the range, exclusive (UTC).</param>
/// <param name="TotalOrders">Orders created in the range, in every channel.</param>
/// <param name="DeliveredOrders">Orders delivered in the range.</param>
/// <param name="Sales">Sales of the range: the total of the orders delivered in it, in MXN.</param>
/// <param name="Channels">Always both channels, Waiter first, so the client can compare them.</param>
/// <param name="SlowestChannel">The channel with the longer average preparation time; null when only one channel has prepared orders or both take the same time.</param>
/// <param name="Trend">The hours that had orders or sales, oldest first.</param>
public sealed record ManagerDashboardResponse(
    DateTime From,
    DateTime To,
    int TotalOrders,
    int DeliveredOrders,
    decimal Sales,
    IReadOnlyList<ChannelMetrics> Channels,
    ManagerChannel? SlowestChannel,
    IReadOnlyList<TrendPoint> Trend);
