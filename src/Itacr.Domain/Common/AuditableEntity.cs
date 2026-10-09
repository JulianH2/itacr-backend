namespace Itacr.Domain.Common;

/// <summary>Base class of every table: key plus UTC audit timestamps, set from <c>TimeProvider</c> by the code that writes the row.</summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
