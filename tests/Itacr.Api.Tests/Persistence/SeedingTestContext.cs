using Itacr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Tests.Persistence;

internal static class SeedingTestContext
{
    /// <summary>An empty in-memory database with the model's HasData rows, shared by every context created with the same name.</summary>
    public static ItacrDbContext CreateDatabase(string name)
    {
        var options = new DbContextOptionsBuilder<ItacrDbContext>().UseInMemoryDatabase(name).Options;
        var context = new ItacrDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
