namespace Itacr.Application.Manager.GetManagerDashboard;

/// <summary>The date range of the dashboard, as two UTC instants: <paramref name="From"/> inclusive and <paramref name="To"/> exclusive.</summary>
public sealed record GetManagerDashboardQuery(DateTimeOffset? From, DateTimeOffset? To);
