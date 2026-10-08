using Itacr.Application.Catalog;
using Itacr.Application.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        return services;
    }
}
