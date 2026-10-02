using Itacr.Api.Features.Customers;
using Itacr.Api.Features.Employees;
using Itacr.Api.Features.Notifications;
using Itacr.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    private const int TypeMaxLength = 30;

    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(TypeMaxLength);

        builder.HasIndex(notification => notification.Type);
        builder.HasIndex(notification => notification.RecipientEmployeeId);
        builder.HasIndex(notification => notification.RecipientCustomerId);
        builder.HasIndex(notification => notification.OrderId);

        builder.HasOne<Employee>().WithMany().HasForeignKey(notification => notification.RecipientEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(notification => notification.RecipientCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(notification => notification.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
