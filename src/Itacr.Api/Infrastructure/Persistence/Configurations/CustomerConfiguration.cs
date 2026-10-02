using Itacr.Api.Features.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private const int NameMaxLength = 120;
    private const int PhoneMaxLength = 10;

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(customer => customer.Name).HasMaxLength(NameMaxLength);
        builder.Property(customer => customer.Phone).HasMaxLength(PhoneMaxLength);
    }
}
