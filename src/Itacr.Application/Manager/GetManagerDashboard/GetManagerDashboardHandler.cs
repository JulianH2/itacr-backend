using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;

namespace Itacr.Application.Manager.GetManagerDashboard;

/// <summary>
/// The summary of the shift for the manager (HU_16): the orders, the sales, the average time to prepare an order in each channel,
/// the channel that takes longer, and the trend by hour. Everything is added up from the orders; nothing is typed by hand.
/// </summary>
public sealed class GetManagerDashboardHandler(IManagerRepository managerRepository, IValidator<GetManagerDashboardQuery> validator)
{
    public async Task<Result<ManagerDashboardResponse>> HandleAsync(GetManagerDashboardQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } error)
        {
            return error;
        }

        var from = query.From!.Value.UtcDateTime;
        var to = query.To!.Value.UtcDateTime;
        var summary = await managerRepository.GetChannelSummaryAsync(from, to, cancellationToken);
        var trend = await managerRepository.GetSalesTrendAsync(from, to, cancellationToken);

        var channels = summary
            .Select(channel => new ChannelMetrics(
                channel.Channel,
                channel.Orders,
                channel.DeliveredOrders,
                channel.Sales,
                channel.PreparedOrders,
                channel.AveragePreparationSeconds is { } seconds ? (int)Math.Round(seconds) : null))
            .ToList();

        return new ManagerDashboardResponse(
            from,
            to,
            channels.Sum(channel => channel.Orders),
            channels.Sum(channel => channel.DeliveredOrders),
            channels.Sum(channel => channel.Sales),
            channels,
            SlowestChannel(channels),
            [.. trend.Select(point => new TrendPoint(point.HourStart, point.Orders, point.Sales))]);
    }

    /// <summary>The channel with the longer average, only when there is something to compare: both have prepared orders and the times differ.</summary>
    private static ManagerChannel? SlowestChannel(IReadOnlyList<ChannelMetrics> channels)
    {
        var withTime = channels.Where(channel => channel.AveragePreparationSeconds is not null).ToList();
        if (withTime.Count < 2)
        {
            return null;
        }

        var slowest = withTime.MaxBy(channel => channel.AveragePreparationSeconds)!;
        return withTime.Count(channel => channel.AveragePreparationSeconds == slowest.AveragePreparationSeconds) > 1
            ? null
            : slowest.Channel;
    }
}
