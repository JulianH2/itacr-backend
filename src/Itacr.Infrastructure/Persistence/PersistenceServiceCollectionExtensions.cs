using Itacr.Application.Common.Persistence;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itacr.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "Default";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string 'ConnectionStrings:{ConnectionStringName}' is missing. Set it with dotnet user-secrets (see CONTRIBUTING.md, section 1).");
        }

        services.AddSingleton<AuditTimestampsInterceptor>();
        services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
        services.AddDbContext<ItacrDbContext>((serviceProvider, options) => options
            .UseAzureSql(connectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(serviceProvider.GetRequiredService<AuditTimestampsInterceptor>()));
        services.AddScoped<IItacrDbContext>(serviceProvider => serviceProvider.GetRequiredService<ItacrDbContext>());

        return services;
    }
}
