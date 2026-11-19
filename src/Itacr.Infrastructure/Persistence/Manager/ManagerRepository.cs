using Dapper;
using Itacr.Application.Manager;
using Itacr.Domain.Orders;

namespace Itacr.Infrastructure.Persistence.Manager;

internal sealed class ManagerRepository(DbSession session) : IManagerRepository
{
    public async Task<IReadOnlyList<ChannelSummary>> GetChannelSummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ChannelSummaryRow>(
            session.Procedure("dbo.usp_Manager_GetChannelSummary", new { From = from, To = to }, cancellationToken));
        return [.. rows.Select(row => new ChannelSummary(
            Enum.Parse<ManagerChannel>(row.ChannelGroup),
            row.Orders,
            row.PreparedOrders,
            row.AveragePreparationSeconds,
            row.DeliveredOrders,
            row.Sales))];
    }

    public async Task<IReadOnlyList<SalesTrendPoint>> GetSalesTrendAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<TrendRow>(
            session.Procedure("dbo.usp_Manager_GetSalesTrend", new { From = from, To = to }, cancellationToken));
        // datetime2 comes back without a kind; every timestamp is stored in UTC.
        return [.. rows.Select(row => new SalesTrendPoint(DateTime.SpecifyKind(row.HourStart, DateTimeKind.Utc), row.Orders, row.Sales))];
    }

    public async Task<ManagerOrdersPage> GetOrdersAsync(
        DateTime from,
        DateTime to,
        ManagerChannel? channel,
        OrderStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(session.Procedure(
            "dbo.usp_Manager_GetOrders",
            new { From = from, To = to, Channel = channel?.ToString(), Status = status?.ToString(), Skip = skip, Take = take },
            cancellationToken));
        var totalCount = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<OrderRow>();
        return new ManagerOrdersPage(
            totalCount,
            [.. rows.Select(row => new ManagerOrder(
                row.Id,
                row.OrderNumber,
                Enum.Parse<OrderChannel>(row.Channel),
                Enum.Parse<OrderStatus>(row.Status),
                row.TableNumber,
                row.Total,
                DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
                Utc(row.ReadyAt),
                Utc(row.DeliveredAt),
                row.PreparationSeconds))]);
    }

    private static DateTime? Utc(DateTime? value) => value is { } instant ? DateTime.SpecifyKind(instant, DateTimeKind.Utc) : null;

    private sealed record ChannelSummaryRow(
        string ChannelGroup,
        int Orders,
        int PreparedOrders,
        double? AveragePreparationSeconds,
        int DeliveredOrders,
        decimal Sales);

    private sealed record TrendRow(DateTime HourStart, int Orders, decimal Sales);

    private sealed record OrderRow(
        Guid Id,
        int OrderNumber,
        string Channel,
        string Status,
        int? TableNumber,
        decimal Total,
        DateTime CreatedAt,
        DateTime? ReadyAt,
        DateTime? DeliveredAt,
        int? PreparationSeconds);
}
