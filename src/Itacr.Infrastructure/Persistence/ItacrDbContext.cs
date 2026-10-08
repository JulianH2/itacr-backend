using Itacr.Application.Common.Persistence;
using Itacr.Domain.Catalog;
using Itacr.Domain.Customers;
using Itacr.Domain.Identity;
using Itacr.Domain.Notifications;
using Itacr.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Itacr.Infrastructure.Persistence;

public sealed class ItacrDbContext(DbContextOptions<ItacrDbContext> options) : DbContext(options), IItacrDbContext
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
