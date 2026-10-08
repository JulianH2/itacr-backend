using Itacr.Domain.Catalog;
using Itacr.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Infrastructure.Persistence.Configurations;

internal sealed class ComboOptionConfiguration : IEntityTypeConfiguration<ComboOption>
{
    private const int TypeMaxLength = 20;
    private const int NameMaxLength = 60;

    public void Configure(EntityTypeBuilder<ComboOption> builder)
    {
        builder.Property(option => option.Type).HasConversion<string>().HasMaxLength(TypeMaxLength);
        builder.Property(option => option.Name).HasMaxLength(NameMaxLength);
        builder.Property(option => option.PriceAdjustment).HasPrecision(10, 2);

        builder.HasIndex(option => new { option.Type, option.Name }).IsUnique();

        builder.HasData(CatalogSeedData.ComboOptions);
    }
}
