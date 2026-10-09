using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Itacr.Application.Identity;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Auth;

/// <summary>Starts the API against the shared test database, with a fake clock and no rate limits.</summary>
public sealed class AuthApiFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    public const string Password = "Itacr2026!";

    private const string PlaceholderPhone = "7710000000";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly PasswordHasher<Employee> PasswordHasher = new();

    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    public async Task<Employee> AddEmployeeAsync(
        string fullName,
        EmployeeRole role,
        string pin,
        EmployeeStatus status = EmployeeStatus.Active,
        IPasswordHasher<Employee>? pinHasher = null)
    {
        var now = Time.GetUtcNow().UtcDateTime;
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@itacr.test",
            Phone = PlaceholderPhone,
            Role = role,
            Status = status,
            PinHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        employee.PinHash = (pinHasher ?? PasswordHasher).HashPassword(employee, pin);
        employee.PasswordHash = PasswordHasher.HashPassword(employee, Password);

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Employees (Id, FullName, Email, Phone, Role, PinHash, PasswordHash, Status, FailedLoginAttempts, CreatedAt, UpdatedAt)
            VALUES (@Id, @FullName, @Email, @Phone, @Role, @PinHash, @PasswordHash, @Status, 0, @CreatedAt, @UpdatedAt);
            """,
            new
            {
                employee.Id,
                employee.FullName,
                employee.Email,
                employee.Phone,
                Role = employee.Role.ToString(),
                employee.PinHash,
                employee.PasswordHash,
                Status = employee.Status.ToString(),
                employee.CreatedAt,
                employee.UpdatedAt,
            });
        return employee;
    }

    public async Task<int> GetFailedLoginAttemptsAsync(Guid employeeId)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT FailedLoginAttempts FROM dbo.Employees WHERE Id = @EmployeeId;",
            new { EmployeeId = employeeId });
    }

    public async Task<string> GetPinHashAsync(Guid employeeId)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT PinHash FROM dbo.Employees WHERE Id = @EmployeeId;",
            new { EmployeeId = employeeId }) ?? string.Empty;
    }

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync()
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        var notifications = await connection.QueryAsync<Notification>(
            "SELECT Id, Type, RecipientEmployeeId, SubjectEmployeeId, RecipientCustomerId, OrderId, SentAt, ReadAt, CreatedAt, UpdatedAt FROM dbo.Notifications;");
        return [.. notifications];
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
        builder.UseSetting("ConnectionStrings:Default", database.ApiConnectionString);
        builder.UseSetting("Jwt:Secret", ApiFactory.TestJwtSecret);
        builder.UseSetting("RateLimiting:PermitLimit", "10000");
        builder.UseSetting("LoginSecurity:PermitLimit", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
