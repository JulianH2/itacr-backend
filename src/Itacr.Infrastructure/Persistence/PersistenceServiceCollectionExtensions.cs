using Itacr.Application.Common.Persistence;
using Itacr.Domain.Identity;
using Itacr.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);
        services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
        services.AddSingleton(serviceProvider => new EmployeeSeeder(
            serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            serviceProvider.GetRequiredService<IOptions<SeedOptions>>().Value,
            serviceProvider.GetRequiredService<IPasswordHasher<Employee>>(),
            serviceProvider.GetRequiredService<ILogger<EmployeeSeeder>>()));
        services.AddDbContext<ItacrDbContext>((serviceProvider, options) =>
        {
            var seeder = serviceProvider.GetRequiredService<EmployeeSeeder>();
            options
                .UseAzureSql(connectionString, sql => sql.EnableRetryOnFailure())
                .AddInterceptors(serviceProvider.GetRequiredService<AuditTimestampsInterceptor>())
                .UseSeeding((context, _) => seeder.Seed((ItacrDbContext)context))
                .UseAsyncSeeding((context, _, cancellationToken) => seeder.SeedAsync((ItacrDbContext)context, cancellationToken));
        });
        services.AddScoped<IItacrDbContext>(serviceProvider => serviceProvider.GetRequiredService<ItacrDbContext>());

        return services;
    }
}
