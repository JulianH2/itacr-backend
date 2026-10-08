using Itacr.Domain.Catalog;
using Itacr.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private const int NameMaxLength = 60;

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(category => category.Name).HasMaxLength(NameMaxLength);
        builder.HasIndex(category => category.Name).IsUnique();

        builder.HasData(CatalogSeedData.Categories);
    }
}
