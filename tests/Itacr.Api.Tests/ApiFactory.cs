using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Itacr.Api.Tests;

/// <summary>Starts the API with a placeholder connection string: no test opens a real database connection.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string PlaceholderConnectionString =
        "Server=localhost,1433;Database=Tests;User Id=tests;Password=tests;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", PlaceholderConnectionString);
    }
}
