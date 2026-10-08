using Itacr.Domain.Customers;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Itacr.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    private const int TypeMaxLength = 30;

    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(TypeMaxLength);

        builder.HasIndex(notification => notification.Type);
        builder.HasIndex(notification => notification.RecipientEmployeeId);
        builder.HasIndex(notification => notification.SubjectEmployeeId);
        builder.HasIndex(notification => notification.RecipientCustomerId);
        builder.HasIndex(notification => notification.OrderId);

        builder.HasOne<Employee>().WithMany().HasForeignKey(notification => notification.RecipientEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(notification => notification.SubjectEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(notification => notification.RecipientCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(notification => notification.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
