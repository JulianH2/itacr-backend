using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Itacr.Api.Features.Auth.Responses;
using Itacr.Api.Features.Employees;
using Itacr.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Auth;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Itacr2026!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databaseName = Guid.NewGuid().ToString();

    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    public async Task<Employee> AddEmployeeAsync(
        string fullName,
        EmployeeRole role,
        string pin,
        EmployeeStatus status = EmployeeStatus.Active)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ItacrDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>();

        var employee = new Employee
        {
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@itacr.test",
            Phone = "7710000000",
            Role = role,
            Status = status,
            PinHash = string.Empty,
        };
        employee.PinHash = passwordHasher.HashPassword(employee, pin);
        employee.PasswordHash = passwordHasher.HashPassword(employee, Password);

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();
        return employee;
    }

    public async Task<T> QueryAsync<T>(Func<ItacrDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<ItacrDbContext>());
    }

    public async Task<LoginResponse> LoginAsync(HttpClient client, Guid employeeId, string pin)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login/pin", new { employeeId, pin });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json);
        return login ?? throw new InvalidOperationException("The login response was empty.");
    }

    public static void Authorize(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", ApiFactory.PlaceholderConnectionString);
        builder.UseSetting("Jwt:Secret", ApiFactory.TestJwtSecret);
        builder.UseSetting("RateLimiting:PermitLimit", "10000");
        builder.UseSetting("LoginSecurity:PermitLimit", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ItacrDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ItacrDbContext>>();
            services.AddDbContext<ItacrDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
