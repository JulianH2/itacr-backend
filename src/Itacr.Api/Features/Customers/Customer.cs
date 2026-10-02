using Itacr.Api.Common;

namespace Itacr.Api.Features.Customers;

public sealed class Customer : AuditableEntity
{
    public required string Name { get; set; }

    public required string Phone { get; set; }
}
