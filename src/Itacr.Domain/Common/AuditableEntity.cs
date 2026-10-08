namespace Itacr.Domain.Common;

/// <summary>Base class of every table: key plus UTC audit timestamps set by <c>AuditTimestampsInterceptor</c>.</summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
