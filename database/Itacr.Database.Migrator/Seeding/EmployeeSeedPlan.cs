using Itacr.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itacr.Database.Migrator.Seeding;

public sealed record SeedEmployee(string FullName, string Email, string Pin, EmployeeRole Role, EmployeeStatus Status, string? Password);

/// <summary>
/// Decides which employees to seed: Development, or any environment with Seed:TestEmployees (QA), gets four test employees;
/// production gets one initial manager from configuration. Employees that already exist (by email) are skipped, so it can run
/// any number of times.
/// </summary>
public sealed class EmployeeSeedPlan(IOptions<SeedOptions> options, ILogger<EmployeeSeedPlan> logger)
{
    private const int PinLength = 4;
    private const string DevelopmentPassword = "Itacr2026!";

    private static readonly SeedEmployee[] DevelopmentEmployees =
    [
        new("Mesero de Prueba", "mesero@itacr.test", "1111", EmployeeRole.Waiter, EmployeeStatus.Active, DevelopmentPassword),
        new("Cocina de Prueba", "cocina@itacr.test", "2222", EmployeeRole.Kitchen, EmployeeStatus.Active, DevelopmentPassword),
        new("Gerente de Prueba", "gerente@itacr.test", "3333", EmployeeRole.Manager, EmployeeStatus.Active, DevelopmentPassword),
        new("Mesero Pendiente", "pendiente@itacr.test", "4444", EmployeeRole.Waiter, EmployeeStatus.PendingApproval, DevelopmentPassword),
    ];

    public IReadOnlyList<SeedEmployee> Build(bool isDevelopment, IReadOnlySet<string> existingEmails, bool hasManager)
    {
        if (isDevelopment || options.Value.TestEmployees)
        {
            return DevelopmentEmployees.Where(candidate => !existingEmails.Contains(candidate.Email)).ToList();
        }

        return BuildInitialManager(existingEmails, hasManager);
    }

    private List<SeedEmployee> BuildInitialManager(IReadOnlySet<string> existingEmails, bool hasManager)
    {
        if (hasManager)
        {
            return [];
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ManagerName)
            || string.IsNullOrWhiteSpace(settings.ManagerEmail)
            || string.IsNullOrWhiteSpace(settings.ManagerPin))
        {
            logger.LogWarning("No manager exists and Seed:ManagerName, Seed:ManagerEmail or Seed:ManagerPin is missing: the initial manager was not created");
            return [];
        }

        if (settings.ManagerPin.Length != PinLength || !settings.ManagerPin.All(char.IsAsciiDigit))
        {
            logger.LogWarning("Seed:ManagerPin must be exactly {PinLength} digits: the initial manager was not created", PinLength);
            return [];
        }

        if (existingEmails.Contains(settings.ManagerEmail))
        {
            logger.LogWarning("An employee with the Seed:ManagerEmail already exists and is not a manager: the initial manager was not created");
            return [];
        }

        return [new SeedEmployee(settings.ManagerName, settings.ManagerEmail, settings.ManagerPin, EmployeeRole.Manager, EmployeeStatus.Active, Password: null)];
    }
}
