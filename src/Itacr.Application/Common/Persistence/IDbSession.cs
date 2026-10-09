namespace Itacr.Application.Common.Persistence;

/// <summary>
/// The database connection of the current request, shared by every repository. It opens on first use and only
/// starts a transaction when a use case asks for one.
/// </summary>
public interface IDbSession
{
    /// <summary>Runs <paramref name="work"/> in one transaction: it commits when the work completes and rolls back when it throws.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}
