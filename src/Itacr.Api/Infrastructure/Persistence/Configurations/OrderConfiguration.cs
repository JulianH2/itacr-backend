using Itacr.Api.Features.Customers;
using Itacr.Api.Features.Employees;
using Itacr.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int EnumMaxLength = 20;
    private const string OrderNumberSequence = "OrderNumbers";

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(order => order.OrderNumber).UseSequence(OrderNumberSequence);
        builder.Property(order => order.Channel).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(order => order.Total).HasPrecision(10, 2);

        builder.HasIndex(order => order.ClientOrderId).IsUnique();
        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAt);
        builder.HasIndex(order => order.WaiterId);

        builder.HasOne<DiningTable>().WithMany().HasForeignKey(order => order.TableId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(order => order.WaiterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(order => order.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
