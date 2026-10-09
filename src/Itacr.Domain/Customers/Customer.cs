using Itacr.Domain.Common;

namespace Itacr.Domain.Customers;

public sealed class Customer : AuditableEntity
{
    public required string Name { get; set; }

    public required string Phone { get; set; }
}
