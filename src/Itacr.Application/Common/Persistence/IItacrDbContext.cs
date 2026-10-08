using Itacr.Domain.Catalog;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Itacr.Application.Common.Persistence;

/// <summary>Temporary bridge to EF Core until the repositories over Dapper replace it (ITACR-36).</summary>
public interface IItacrDbContext
{
    DbSet<Employee> Employees { get; }

    DbSet<Category> Categories { get; }

    DbSet<Notification> Notifications { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
