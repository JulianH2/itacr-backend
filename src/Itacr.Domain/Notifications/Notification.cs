using Itacr.Domain.Common;

namespace Itacr.Domain.Notifications;

public sealed class Notification : AuditableEntity
{
    public NotificationType Type { get; set; }

    public Guid? RecipientEmployeeId { get; set; }

    public Guid? SubjectEmployeeId { get; set; }

    public Guid? RecipientCustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    /// <summary>When a manager answered a PIN reset request (HU_15); null while it is pending.</summary>
    public DateTime? ResolvedAt { get; set; }
}
