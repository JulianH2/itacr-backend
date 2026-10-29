using Itacr.Application.Identity;
using Itacr.Infrastructure.Identity;
using Itacr.Infrastructure.Notifications;
using Itacr.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddJwtAuthentication();
        services.AddPushNotifications();
        services.AddSingleton<ICredentialHasher, CredentialHasher>();

        return services;
    }
}
