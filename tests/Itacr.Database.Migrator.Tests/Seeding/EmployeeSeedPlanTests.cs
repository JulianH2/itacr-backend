using Itacr.Database.Migrator.Seeding;
using Itacr.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Itacr.Database.Migrator.Tests.Seeding;

public sealed class EmployeeSeedPlanTests
{
    private static readonly SeedOptions ValidManager = new()
    {
        ManagerName = "Gerente Inicial",
        ManagerEmail = "gerente@itacr.example",
        ManagerPin = "4821",
    };

    private static readonly HashSet<string> NoEmails = [];

    [Fact]
    public void Build_ShouldReturnAnActiveEmployeePerRoleAndAPendingWaiter_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        var employees = plan.Build(isDevelopment: true, NoEmails, hasManager: false);

        // Assert
        Assert.Equal(4, employees.Count);
        var active = employees.Where(employee => employee.Status == EmployeeStatus.Active).ToList();
        Assert.Equivalent(new[] { EmployeeRole.Waiter, EmployeeRole.Kitchen, EmployeeRole.Manager }, active.Select(employee => employee.Role));
        var pending = Assert.Single(employees, employee => employee.Status == EmployeeStatus.PendingApproval);
        Assert.Equal(EmployeeRole.Waiter, pending.Role);
        Assert.All(employees, employee => Assert.Equal("Itacr2026!", employee.Password));
    }

    [Fact]
    public void Build_ShouldSkipEmployeesThatAlreadyExist_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var plan = CreatePlan();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MESERO@itacr.test", "cocina@itacr.test" };

        // Act
        var employees = plan.Build(isDevelopment: true, existing, hasManager: true);

        // Assert
        Assert.Equivalent(new[] { "gerente@itacr.test", "pendiente@itacr.test" }, employees.Select(employee => employee.Email));
    }

    [Fact]
    public void Build_ShouldReturnTheTestEmployees_WhenTestEmployeesIsEnabledOutsideDevelopment()
    {
        // Arrange
        var plan = CreatePlan(new SeedOptions { TestEmployees = true });

        // Act
        var employees = plan.Build(isDevelopment: false, NoEmails, hasManager: false);

        // Assert
        Assert.Equivalent(
            new[] { "mesero@itacr.test", "cocina@itacr.test", "gerente@itacr.test", "pendiente@itacr.test" },
            employees.Select(employee => employee.Email));
    }

    [Fact]
    public void Build_ShouldReturnOnlyTheConfiguredManager_WhenEnvironmentIsProduction()
    {
        // Arrange
        var plan = CreatePlan(ValidManager);

        // Act
        var employees = plan.Build(isDevelopment: false, NoEmails, hasManager: false);

        // Assert
        var manager = Assert.Single(employees);
        Assert.Equal(EmployeeRole.Manager, manager.Role);
        Assert.Equal(EmployeeStatus.Active, manager.Status);
        Assert.Equal(ValidManager.ManagerEmail, manager.Email);
        Assert.Equal(ValidManager.ManagerPin, manager.Pin);
        Assert.Null(manager.Password);
    }

    [Fact]
    public void Build_ShouldReturnNothing_WhenProductionSettingsAreMissing()
    {
        // Arrange
        var plan = CreatePlan();

        // Act
        var employees = plan.Build(isDevelopment: false, NoEmails, hasManager: false);

        // Assert
        Assert.Empty(employees);
    }

    [Fact]
    public void Build_ShouldReturnNothing_WhenProductionPinIsNotFourDigits()
    {
        // Arrange
        var plan = CreatePlan(new SeedOptions { ManagerName = "Gerente", ManagerEmail = "g@itacr.example", ManagerPin = "12a4" });

        // Act
        var employees = plan.Build(isDevelopment: false, NoEmails, hasManager: false);

        // Assert
        Assert.Empty(employees);
    }

    [Fact]
    public void Build_ShouldReturnNothing_WhenAManagerAlreadyExists()
    {
        // Arrange
        var plan = CreatePlan(ValidManager);

        // Act
        var employees = plan.Build(isDevelopment: false, NoEmails, hasManager: true);

        // Assert
        Assert.Empty(employees);
    }

    [Fact]
    public void Build_ShouldReturnNothing_WhenTheManagerEmailBelongsToAnotherEmployee()
    {
        // Arrange
        var plan = CreatePlan(ValidManager);
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GERENTE@itacr.example" };

        // Act
        var employees = plan.Build(isDevelopment: false, existing, hasManager: false);

        // Assert
        Assert.Empty(employees);
    }

    private static EmployeeSeedPlan CreatePlan(SeedOptions? options = null) =>
        new(Options.Create(options ?? new SeedOptions()), NullLogger<EmployeeSeedPlan>.Instance);
}
