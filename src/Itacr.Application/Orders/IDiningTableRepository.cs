using Itacr.Domain.Orders;

namespace Itacr.Application.Orders;

public interface IDiningTableRepository
{
    /// <summary>Every dining table ordered by number.</summary>
    Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken);
}
