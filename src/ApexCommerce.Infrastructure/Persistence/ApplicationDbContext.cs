using MediatR;
using Microsoft.EntityFrameworkCore;
using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher? _publisher;

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditTimestamps();
        var domainEvents = ExtractDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events after atomic DB transaction completes
        if (_publisher != null && domainEvents.Count > 0)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
    }

    private void UpdateAuditTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().BaseType is not null && e.State is EntityState.Modified);

        foreach (var entry in entries)
        {
            var updatedAtProp = entry.Property("UpdatedAtUtc");
            if (updatedAtProp != null)
            {
                updatedAtProp.CurrentValue = DateTime.UtcNow;
            }
        }
    }

    private List<IDomainEvent> ExtractDomainEvents()
    {
        var aggregateRoots = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().BaseType is not null)
            .Select(e => e.Entity)
            .OfType<AggregateRoot<Guid>>()
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregateRoots)
        {
            aggregate.ClearDomainEvents();
        }

        return domainEvents;
    }
}

