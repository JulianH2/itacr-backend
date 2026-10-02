using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Itacr.Api.Tests;

/// <summary>Starts the API with a placeholder connection string: no test opens a real database connection.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string PlaceholderConnectionString =
        "Server=localhost,1433;Database=Tests;User Id=tests;Password=tests;TrustServerCertificate=True";

    public const string TestJwtSecret = "itacr-tests-jwt-secret-that-is-long-enough-for-hmac-sha256-signing-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", PlaceholderConnectionString);
        builder.UseSetting("Jwt:Secret", TestJwtSecret);
    }
}
