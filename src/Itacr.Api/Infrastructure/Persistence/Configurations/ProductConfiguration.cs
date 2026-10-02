using Itacr.Api.Features.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int NameMaxLength = 100;
    private const int DescriptionMaxLength = 500;
    private const int ImageUrlMaxLength = 500;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(product => product.Name).HasMaxLength(NameMaxLength);
        builder.Property(product => product.Description).HasMaxLength(DescriptionMaxLength);
        builder.Property(product => product.ImageUrl).HasMaxLength(ImageUrlMaxLength);
        builder.Property(product => product.Price).HasPrecision(10, 2);

        builder.HasIndex(product => product.CategoryId);

        builder.HasOne<Category>().WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
