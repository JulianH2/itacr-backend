using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Itacr.Api.Common.Http;
using Itacr.Api.Common.Options;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Itacr.Api.Common;

public static class ApiServiceCollectionExtensions
{
    private const string UnknownClient = "unknown";

    public static IServiceCollection AddApiBase(this IServiceCollection services)
    {
        services.AddRouting(options => options.LowercaseUrls = true);
        services
            .AddControllers(options =>
            {
                options.Filters.Add(new AuthorizeFilter());

                // Required values are checked by the use case validators, with Spanish messages.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestResponse.Create);

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddSingleton(TimeProvider.System);
        services.AddHealthChecks();

        services.AddValidatedOptions<CorsOptions>(CorsOptions.SectionName);
        services.AddValidatedOptions<RateLimitingOptions>(RateLimitingOptions.SectionName);
        services.AddCors();
        services.AddRateLimiter();
        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>(ConfigureRateLimiter);

        return services;
    }

    private static void AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void ConfigureRateLimiter(RateLimiterOptions limiter, IOptions<RateLimitingOptions> settings)
    {
        var rateLimiting = settings.Value;
        limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimiting.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds),
                    QueueLimit = 0,
                }));
    }
}
