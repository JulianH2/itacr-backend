namespace Itacr.Application.Kitchen;

public interface IKitchenRepository
{
    /// <summary>The orders in New, InPreparation, or Ready, oldest first. Delivered orders are not part of the queue.</summary>
    Task<IReadOnlyList<ActiveOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken);
}
