using Itacr.Api.Features.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds employees at runtime because their PINs must be hashed and differ per environment.
/// Development gets three test employees; any other environment gets one initial manager from configuration.
/// Every run first checks what already exists by email, so running it again never duplicates rows.
/// </summary>
public sealed class EmployeeSeeder(
    bool isDevelopment,
    SeedOptions options,
    IPasswordHasher<Employee> passwordHasher,
    ILogger<EmployeeSeeder> logger)
{
    private const int PinLength = 4;
    private const string PlaceholderPhone = "0000000000";

    private static readonly (string FullName, string Email, string Pin, EmployeeRole Role)[] DevelopmentEmployees =
    [
        ("Mesero de Prueba", "mesero@itacr.test", "1111", EmployeeRole.Waiter),
        ("Cocina de Prueba", "cocina@itacr.test", "2222", EmployeeRole.Kitchen),
        ("Gerente de Prueba", "gerente@itacr.test", "3333", EmployeeRole.Manager),
    ];

    public void Seed(ItacrDbContext context)
    {
        var existingEmails = context.Employees.Select(employee => employee.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasManager = context.Employees.Any(employee => employee.Role == EmployeeRole.Manager);

        var newEmployees = BuildEmployees(existingEmails, hasManager);
        if (newEmployees.Count == 0)
        {
            return;
        }

        context.Employees.AddRange(newEmployees);
        context.SaveChanges();
    }

    public async Task SeedAsync(ItacrDbContext context, CancellationToken cancellationToken)
    {
        var existingEmails = (await context.Employees.Select(employee => employee.Email).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasManager = await context.Employees.AnyAsync(employee => employee.Role == EmployeeRole.Manager, cancellationToken);

        var newEmployees = BuildEmployees(existingEmails, hasManager);
        if (newEmployees.Count == 0)
        {
            return;
        }

        context.Employees.AddRange(newEmployees);
        await context.SaveChangesAsync(cancellationToken);
    }

    private List<Employee> BuildEmployees(HashSet<string> existingEmails, bool hasManager)
    {
        if (isDevelopment)
        {
            return DevelopmentEmployees
                .Where(candidate => !existingEmails.Contains(candidate.Email))
                .Select(candidate => NewEmployee(candidate.FullName, candidate.Email, candidate.Pin, candidate.Role, PlaceholderPhone))
                .ToList();
        }

        return BuildInitialManager(existingEmails, hasManager);
    }

    private List<Employee> BuildInitialManager(HashSet<string> existingEmails, bool hasManager)
    {
        if (hasManager)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(options.ManagerName)
            || string.IsNullOrWhiteSpace(options.ManagerEmail)
            || string.IsNullOrWhiteSpace(options.ManagerPin))
        {
            logger.LogWarning("No manager exists and Seed:ManagerName, Seed:ManagerEmail or Seed:ManagerPin is missing: the initial manager was not created");
            return [];
        }

        if (options.ManagerPin.Length != PinLength || !options.ManagerPin.All(char.IsAsciiDigit))
        {
            logger.LogWarning("Seed:ManagerPin must be exactly {PinLength} digits: the initial manager was not created", PinLength);
            return [];
        }

        if (existingEmails.Contains(options.ManagerEmail))
        {
            logger.LogWarning("An employee with the Seed:ManagerEmail already exists and is not a manager: the initial manager was not created");
            return [];
        }

        return [NewEmployee(options.ManagerName, options.ManagerEmail, options.ManagerPin, EmployeeRole.Manager, PlaceholderPhone)];
    }

    private Employee NewEmployee(string fullName, string email, string pin, EmployeeRole role, string phone)
    {
        var employee = new Employee
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            Role = role,
            Status = EmployeeStatus.Active,
            PinHash = string.Empty,
        };
        employee.PinHash = passwordHasher.HashPassword(employee, pin);
        return employee;
    }
}
