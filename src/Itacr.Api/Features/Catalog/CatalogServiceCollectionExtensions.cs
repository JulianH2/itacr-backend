namespace Itacr.Api.Features.Catalog;

public static class CatalogServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogFeature(this IServiceCollection services)
    {
        services.AddScoped<ICatalogService, CatalogService>();
        return services;
    }
}
