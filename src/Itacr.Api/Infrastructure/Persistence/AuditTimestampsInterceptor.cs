using Itacr.Api.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Itacr.Api.Infrastructure.Persistence;

/// <summary>Sets CreatedAt and UpdatedAt (UTC, from the injected TimeProvider) on every save. A CreatedAt already set by the caller, such as the original time of an offline order, is kept.</summary>
public sealed class AuditTimestampsInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context?.ChangeTracker, timeProvider.GetUtcNow().UtcDateTime);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context?.ChangeTracker, timeProvider.GetUtcNow().UtcDateTime);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public static void Stamp(ChangeTracker? changeTracker, DateTime utcNow)
    {
        if (changeTracker is null)
        {
            return;
        }

        foreach (var entry in changeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
