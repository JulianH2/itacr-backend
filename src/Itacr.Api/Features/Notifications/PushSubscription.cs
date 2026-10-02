using Itacr.Api.Common;

namespace Itacr.Api.Features.Notifications;

public sealed class PushSubscription : AuditableEntity
{
    public Guid? EmployeeId { get; set; }

    public Guid? CustomerId { get; set; }

    public required string Endpoint { get; set; }

    public required string P256dh { get; set; }

    public required string Auth { get; set; }
}
