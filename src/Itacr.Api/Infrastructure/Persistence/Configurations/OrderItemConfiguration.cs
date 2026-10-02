using Itacr.Api.Features.Catalog;
using Itacr.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    private const int OptionNameMaxLength = 60;

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(item => item.UnitPrice).HasPrecision(10, 2);
        builder.Property(item => item.Subtotal).HasPrecision(10, 2);
        builder.Property(item => item.Size).HasMaxLength(OptionNameMaxLength);
        builder.Property(item => item.Drink).HasMaxLength(OptionNameMaxLength);
        builder.Property(item => item.Extras).HasColumnType("json");
        builder.Property(item => item.Exclusions).HasColumnType("json");

        builder.HasIndex(item => item.OrderId);
        builder.HasIndex(item => item.ProductId);

        builder.HasOne<Order>().WithMany(order => order.Items).HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
