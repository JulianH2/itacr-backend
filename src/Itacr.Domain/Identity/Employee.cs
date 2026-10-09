using Itacr.Domain.Common;

namespace Itacr.Domain.Identity;

public sealed class Employee : AuditableEntity
{
    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required string Phone { get; set; }

    public EmployeeRole Role { get; set; }

    public required string PinHash { get; set; }

    public string? PasswordHash { get; set; }

    public EmployeeStatus Status { get; set; }

    public Guid? ApprovedById { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutEndAt { get; set; }
}
