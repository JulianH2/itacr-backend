using Itacr.Api.Features.Catalog;
using Itacr.Api.Features.Customers;
using Itacr.Api.Features.Employees;
using Itacr.Api.Features.Notifications;
using Itacr.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Infrastructure.Persistence;

public sealed class ItacrDbContext(DbContextOptions<ItacrDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ComboOption> ComboOptions => Set<ComboOption>();

    public DbSet<DiningTable> DiningTables => Set<DiningTable>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItacrDbContext).Assembly);
    }
}
