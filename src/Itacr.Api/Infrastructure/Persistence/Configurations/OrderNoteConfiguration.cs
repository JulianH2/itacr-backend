using Itacr.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    private const int TextMaxLength = 500;

    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.Property(note => note.Text).HasMaxLength(TextMaxLength);
        builder.HasIndex(note => note.OrderId);

        builder.HasOne<Order>().WithMany(order => order.Notes).HasForeignKey(note => note.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
