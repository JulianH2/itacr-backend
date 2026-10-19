using System.Data;
using Dapper;
using Itacr.Application.Catalog;
using Itacr.Application.Common.Persistence;
using Itacr.Application.Identity;
using Itacr.Application.Kitchen;
using Itacr.Application.Notifications;
using Itacr.Application.Orders;
using Itacr.Domain.Identity;
using Itacr.Infrastructure.Persistence.Catalog;
using Itacr.Infrastructure.Persistence.Identity;
using Itacr.Infrastructure.Persistence.Kitchen;
using Itacr.Infrastructure.Persistence.Notifications;
using Itacr.Infrastructure.Persistence.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "Default";
    private const string HealthCheckName = "database";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string 'ConnectionStrings:{ConnectionStringName}' is missing. Set it with dotnet user-secrets (see CONTRIBUTING.md, section 1).");
        }

        // Every DateTime column is datetime2; Dapper's default (datetime) would drop precision and reject dates before 1753.
        SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
        SqlMapper.AddTypeMap(typeof(DateTime?), DbType.DateTime2);

        services.AddSingleton(new DatabaseConnection(connectionString));
        services.AddScoped<DbSession>();
        services.AddScoped<IDbSession>(serviceProvider => serviceProvider.GetRequiredService<DbSession>());

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDiningTableRepository, DiningTableRepository>();
        services.AddScoped<IKitchenRepository, KitchenRepository>();
        services.AddScoped<IPickupOrderRepository, PickupOrderRepository>();
        services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(HealthCheckName);

        return services;
    }
}
