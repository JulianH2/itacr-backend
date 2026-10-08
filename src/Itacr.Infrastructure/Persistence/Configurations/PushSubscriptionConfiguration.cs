using Itacr.Domain.Customers;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    private const int EndpointMaxLength = 450;
    private const int KeyMaxLength = 200;

    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.Property(subscription => subscription.Endpoint).HasMaxLength(EndpointMaxLength);
        builder.Property(subscription => subscription.P256dh).HasMaxLength(KeyMaxLength);
        builder.Property(subscription => subscription.Auth).HasMaxLength(KeyMaxLength);

        builder.HasIndex(subscription => subscription.Endpoint).IsUnique();
        builder.HasIndex(subscription => subscription.EmployeeId);
        builder.HasIndex(subscription => subscription.CustomerId);

        builder.HasOne<Employee>().WithMany().HasForeignKey(subscription => subscription.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(subscription => subscription.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
