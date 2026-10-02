using Itacr.Api.Common.Options;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Itacr.Api.Common;

public static class WebApplicationExtensions
{
    private const string HealthRoute = "/api/v1/health";

    public static WebApplication UseApiBase(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSecurityHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        var origins = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value.Origins;
        app.UseCors(policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks(HealthRoute);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        return app;
    }

    private static void UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.Append("Referrer-Policy", "no-referrer");
            await next(context);
        });
    }
}
