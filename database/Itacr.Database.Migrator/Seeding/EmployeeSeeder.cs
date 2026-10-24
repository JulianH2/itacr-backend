using Dapper;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Itacr.Database.Migrator.Seeding;

/// <summary>Seeds employees in C# because their PINs and passwords are hashed with the same <see cref="PasswordHasher{TUser}"/> the API verifies with.</summary>
public sealed class EmployeeSeeder(
    EmployeeSeedPlan plan,
    IPasswordHasher<Employee> passwordHasher,
    TimeProvider timeProvider,
    ILogger<EmployeeSeeder> logger)
{
    private const string PlaceholderPhone = "0000000000";

    public async Task SeedAsync(string connectionString, bool isDevelopment, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var emails = await connection.QueryAsync<string>(new CommandDefinition(
            "SELECT Email FROM dbo.Employees;",
            cancellationToken: cancellationToken));
        var hasManager = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Employees WHERE Role = @Role) THEN 1 ELSE 0 END AS bit);",
            new { Role = nameof(EmployeeRole.Manager) },
            cancellationToken: cancellationToken));

        var withoutPassword = (await connection.QueryAsync<(Guid Id, string Email)>(new CommandDefinition(
            "SELECT Id, Email FROM dbo.Employees WHERE PasswordHash IS NULL;",
            cancellationToken: cancellationToken))).ToDictionary(employee => employee.Email, employee => employee.Id, StringComparer.OrdinalIgnoreCase);

        var newEmployees = plan.Build(isDevelopment, emails.ToHashSet(StringComparer.OrdinalIgnoreCase), hasManager);
        var missingPasswords = plan.BuildMissingPasswords(isDevelopment, withoutPassword.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (newEmployees.Count == 0 && missingPasswords.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var candidate in newEmployees)
        {
            var employee = ToEmployee(candidate, now);
            await connection.ExecuteAsync(new CommandDefinition(
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
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (var candidate in missingPasswords)
        {
            var id = withoutPassword[candidate.Email];
            var passwordHash = passwordHasher.HashPassword(Unused, candidate.Password!);
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Employees SET PasswordHash = @PasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @Id AND PasswordHash IS NULL;",
                new { Id = id, PasswordHash = passwordHash, UpdatedAt = now },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} employees and set the test password of {Updated}", newEmployees.Count, missingPasswords.Count);
    }

    // PasswordHasher<TUser> never reads the user: the hash depends only on its options.
    private static readonly Employee Unused = new() { FullName = string.Empty, Email = string.Empty, Phone = string.Empty, PinHash = string.Empty };

    private Employee ToEmployee(SeedEmployee candidate, DateTime now)
    {
        var employee = new Employee
        {
            Id = Guid.CreateVersion7(),
            FullName = candidate.FullName,
            Email = candidate.Email,
            Phone = PlaceholderPhone,
            Role = candidate.Role,
            Status = candidate.Status,
            PinHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        employee.PinHash = passwordHasher.HashPassword(employee, candidate.Pin);
        if (candidate.Password is not null)
        {
            employee.PasswordHash = passwordHasher.HashPassword(employee, candidate.Password);
        }

        return employee;
    }
}
