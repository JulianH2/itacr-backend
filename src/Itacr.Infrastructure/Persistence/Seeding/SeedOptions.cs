namespace Itacr.Infrastructure.Persistence.Seeding;

/// <summary>Initial manager for production, read from the "Seed" section (environment variables Seed__ManagerName, Seed__ManagerEmail, Seed__ManagerPin).</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string? ManagerName { get; init; }

    public string? ManagerEmail { get; init; }

    public string? ManagerPin { get; init; }
}
