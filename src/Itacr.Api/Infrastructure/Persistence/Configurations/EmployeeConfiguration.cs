using Itacr.Api.Features.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itacr.Api.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    private const int FullNameMaxLength = 120;
    private const int EmailMaxLength = 254;
    private const int PhoneMaxLength = 20;
    private const int EnumMaxLength = 20;
    private const int HashMaxLength = 255;

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(employee => employee.FullName).HasMaxLength(FullNameMaxLength);
        builder.Property(employee => employee.Email).HasMaxLength(EmailMaxLength);
        builder.Property(employee => employee.Phone).HasMaxLength(PhoneMaxLength);
        builder.Property(employee => employee.Role).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(employee => employee.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(employee => employee.PinHash).HasMaxLength(HashMaxLength);
        builder.Property(employee => employee.PasswordHash).HasMaxLength(HashMaxLength);

        builder.HasIndex(employee => employee.Email).IsUnique();
        builder.HasIndex(employee => employee.Status);

        builder.HasOne<Employee>().WithMany().HasForeignKey(employee => employee.ApprovedById).OnDelete(DeleteBehavior.Restrict);
    }
}
