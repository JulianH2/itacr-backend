using Itacr.Api.Common;

namespace Itacr.Api.Features.Notifications;

public sealed class Notification : AuditableEntity
{
    public NotificationType Type { get; set; }

    public Guid? RecipientEmployeeId { get; set; }

    public Guid? SubjectEmployeeId { get; set; }

    public Guid? RecipientCustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
