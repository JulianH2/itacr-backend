namespace Itacr.Database.Migrator.Seeding;

/// <summary>
/// Read from the "Seed" section: the initial manager for production (Seed__ManagerName, Seed__ManagerEmail, Seed__ManagerPin),
/// and Seed__TestEmployees, which gives a non-production environment such as QA the development test employees.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool TestEmployees { get; init; }

    public string? ManagerName { get; init; }

    public string? ManagerEmail { get; init; }

    public string? ManagerPin { get; init; }
}
