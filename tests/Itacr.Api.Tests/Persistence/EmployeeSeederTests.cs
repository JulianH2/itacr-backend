using Itacr.Api.Features.Employees;
using Itacr.Api.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Tests.Persistence;

public sealed class EmployeeSeederTests
{
    private static readonly SeedOptions ValidManager = new()
    {
        ManagerName = "Gerente Inicial",
        ManagerEmail = "gerente@itacr.example",
        ManagerPin = "4821",
    };

    [Fact]
    public void Seed_ShouldCreateThreeActiveEmployees_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldCreateThreeActiveEmployees_WhenEnvironmentIsDevelopment));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: true);

        // Act
        seeder.Seed(context);

        // Assert
        var employees = context.Employees.ToList();
        Assert.Equal(3, employees.Count);
        Assert.All(employees, employee => Assert.Equal(EmployeeStatus.Active, employee.Status));
        Assert.Equivalent(new[] { EmployeeRole.Waiter, EmployeeRole.Kitchen, EmployeeRole.Manager }, employees.Select(employee => employee.Role));
    }

    [Fact]
    public void Seed_ShouldKeepTheSameRowCount_WhenItRunsTwice()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldKeepTheSameRowCount_WhenItRunsTwice));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: true);

        // Act
        seeder.Seed(context);
        var countAfterFirstRun = context.Employees.Count();
        seeder.Seed(context);

        // Assert
        Assert.Equal(3, countAfterFirstRun);
        Assert.Equal(countAfterFirstRun, context.Employees.Count());
    }

    [Fact]
    public async Task SeedAsync_ShouldKeepTheSameRowCount_WhenItRunsTwice()
    {
        // Arrange
        await using var context = SeedingTestContext.CreateDatabase(nameof(SeedAsync_ShouldKeepTheSameRowCount_WhenItRunsTwice));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: true);

        // Act
        await seeder.SeedAsync(context, CancellationToken.None);
        await seeder.SeedAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(3, await context.Employees.CountAsync());
    }

    [Fact]
    public void Seed_ShouldNeverStorePinsInPlainText_WhenEmployeesAreCreated()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldNeverStorePinsInPlainText_WhenEmployeesAreCreated));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: true);
        string[] testPins = ["1111", "2222", "3333"];

        // Act
        seeder.Seed(context);

        // Assert
        var employees = context.Employees.ToList();
        Assert.All(employees, employee => Assert.DoesNotContain(employee.PinHash, testPins));
        Assert.All(employees, employee => Assert.True(employee.PinHash.Length > 4));
        var waiter = employees.Single(employee => employee.Role == EmployeeRole.Waiter);
        Assert.Equal(PasswordVerificationResult.Success, SeedingTestContext.PasswordHasher.VerifyHashedPassword(waiter, waiter.PinHash, "1111"));
    }

    [Fact]
    public void Seed_ShouldCreateOnlyTheConfiguredManager_WhenEnvironmentIsProduction()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldCreateOnlyTheConfiguredManager_WhenEnvironmentIsProduction));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: false, ValidManager);

        // Act
        seeder.Seed(context);
        seeder.Seed(context);

        // Assert
        var manager = Assert.Single(context.Employees.ToList());
        Assert.Equal(EmployeeRole.Manager, manager.Role);
        Assert.Equal(ValidManager.ManagerEmail, manager.Email);
        Assert.NotEqual(ValidManager.ManagerPin, manager.PinHash);
        Assert.Equal(PasswordVerificationResult.Success, SeedingTestContext.PasswordHasher.VerifyHashedPassword(manager, manager.PinHash, "4821"));
    }

    [Fact]
    public void Seed_ShouldCreateNothing_WhenProductionSettingsAreMissing()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldCreateNothing_WhenProductionSettingsAreMissing));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: false);

        // Act
        seeder.Seed(context);

        // Assert
        Assert.Empty(context.Employees);
    }

    [Fact]
    public void Seed_ShouldCreateNothing_WhenProductionPinIsNotFourDigits()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldCreateNothing_WhenProductionPinIsNotFourDigits));
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: false, new SeedOptions { ManagerName = "Gerente", ManagerEmail = "g@itacr.example", ManagerPin = "12" });

        // Act
        seeder.Seed(context);

        // Assert
        Assert.Empty(context.Employees);
    }

    [Fact]
    public void Seed_ShouldNotCreateAnotherManager_WhenAManagerAlreadyExists()
    {
        // Arrange
        using var context = SeedingTestContext.CreateDatabase(nameof(Seed_ShouldNotCreateAnotherManager_WhenAManagerAlreadyExists));
        context.Employees.Add(new Employee { FullName = "Existente", Email = "existente@itacr.example", Phone = "7711111111", Role = EmployeeRole.Manager, Status = EmployeeStatus.Active, PinHash = "hash" });
        context.SaveChanges();
        var seeder = SeedingTestContext.CreateSeeder(isDevelopment: false, ValidManager);

        // Act
        seeder.Seed(context);

        // Assert
        Assert.Single(context.Employees);
    }
}
