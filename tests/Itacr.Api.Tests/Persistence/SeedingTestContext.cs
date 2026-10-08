using Itacr.Domain.Identity;
using Itacr.Infrastructure.Persistence;
using Itacr.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itacr.Api.Tests.Persistence;

internal static class SeedingTestContext
{
    public static readonly PasswordHasher<Employee> PasswordHasher = new();

    /// <summary>An empty in-memory database with the model's HasData rows, shared by every context created with the same name.</summary>
    public static ItacrDbContext CreateDatabase(string name)
    {
        var options = new DbContextOptionsBuilder<ItacrDbContext>().UseInMemoryDatabase(name).Options;
        var context = new ItacrDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static EmployeeSeeder CreateSeeder(bool isDevelopment, SeedOptions? options = null) =>
        new(isDevelopment, options ?? new SeedOptions(), PasswordHasher, NullLogger<EmployeeSeeder>.Instance);
}
