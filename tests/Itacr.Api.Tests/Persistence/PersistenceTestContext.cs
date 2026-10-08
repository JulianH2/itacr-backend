using Itacr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Tests.Persistence;

internal static class PersistenceTestContext
{
    private const string UnusedConnectionString = "Server=localhost,1433;Database=Tests;User Id=tests;Password=tests;TrustServerCertificate=True";

    /// <summary>Builds a context that never connects: tests only inspect the model and the change tracker.</summary>
    public static ItacrDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ItacrDbContext>()
            .UseSqlServer(UnusedConnectionString)
            .Options;
        return new ItacrDbContext(options);
    }
}
