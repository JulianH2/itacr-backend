using Itacr.Domain.Identity;

namespace Itacr.Application.Identity;

public sealed record EmployeeSummary(Guid Id, string FullName);

public sealed record FailedPinLogin(bool IsLocked, DateTime? LockoutEndAt);

public interface IEmployeeRepository
{
    Task<IReadOnlyList<EmployeeSummary>> GetActiveByRoleAsync(EmployeeRole role, CancellationToken cancellationToken);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Counts a wrong PIN and, when it reaches <paramref name="maxFailedAttempts"/>, locks the PIN sign-in until <paramref name="lockoutEndAt"/>, atomically.</summary>
    Task<FailedPinLogin> RegisterFailedPinLoginAsync(
        Guid id,
        int maxFailedAttempts,
        DateTime lockoutEndAt,
        DateTime now,
        CancellationToken cancellationToken);

    /// <summary>Clears the failed count and the lockout, and stores <paramref name="newPinHash"/> when it is not null.</summary>
    Task RegisterSuccessfulPinLoginAsync(Guid id, string? newPinHash, DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetActiveManagerIdsAsync(Guid excludedEmployeeId, CancellationToken cancellationToken);
}
