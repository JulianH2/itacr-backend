using System.Threading.RateLimiting;
using Itacr.Application.Orders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Itacr.Api.Common;

/// <summary>
/// The limits of the public endpoints for pickup orders (ITACR-42): their options, validated at startup, and one rate limit
/// policy for creating orders and another for following them. Each client IP address has its own window, on top of the global limit.
/// </summary>
public static class PublicOrdersServiceCollectionExtensions
{
    public const string CreateRateLimitPolicy = "public-order-create";
    public const string StatusRateLimitPolicy = "public-order-status";

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddPublicOrders(this IServiceCollection services)
    {
        services.AddOptions<PublicOrdersOptions>().BindConfiguration(PublicOrdersOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<PublicOrdersOptions>>(AddPolicies);

        return services;
    }

    private static void AddPolicies(RateLimiterOptions limiter, IOptions<PublicOrdersOptions> settings)
    {
        var options = settings.Value;
        limiter.AddPolicy(CreateRateLimitPolicy, context => ClientWindow(context, options.CreatePermitLimit, options.CreateWindowSeconds));
        limiter.AddPolicy(StatusRateLimitPolicy, context => ClientWindow(context, options.StatusPermitLimit, options.StatusWindowSeconds));
    }

    private static RateLimitPartition<string> ClientWindow(HttpContext context, int permitLimit, int windowSeconds) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            });
}
