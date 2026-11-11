using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.SessionId).HasMaxLength(150);
        builder.HasIndex(c => c.SessionId);
        builder.HasIndex(c => c.CustomerId);

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(10).IsRequired();
        });
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.CustomerId).IsRequired();
        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(255);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.OwnsOne(o => o.ShippingAddress, addr =>
        {
            addr.Property(a => a.Street).HasColumnName("shipping_street").HasMaxLength(200).IsRequired();
            addr.Property(a => a.City).HasColumnName("shipping_city").HasMaxLength(100).IsRequired();
            addr.Property(a => a.State).HasColumnName("shipping_state").HasMaxLength(100);
            addr.Property(a => a.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20).IsRequired();
            addr.Property(a => a.Country).HasColumnName("shipping_country").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(o => o.BillingAddress, addr =>
        {
            addr.Property(a => a.Street).HasColumnName("billing_street").HasMaxLength(200);
            addr.Property(a => a.City).HasColumnName("billing_city").HasMaxLength(100);
            addr.Property(a => a.State).HasColumnName("billing_state").HasMaxLength(100);
            addr.Property(a => a.PostalCode).HasColumnName("billing_postal_code").HasMaxLength(20);
            addr.Property(a => a.Country).HasColumnName("billing_country").HasMaxLength(100);
        });

        builder.OwnsOne(o => o.SubTotal, p =>
        {
            p.Property(m => m.Amount).HasColumnName("subtotal_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("subtotal_currency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(o => o.DiscountAmount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("discount_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("discount_currency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(o => o.TaxAmount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("tax_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("tax_currency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(o => o.ShippingCost, p =>
        {
            p.Property(m => m.Amount).HasColumnName("shipping_cost_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("shipping_cost_currency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(o => o.TotalAmount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("total_currency").HasMaxLength(10).IsRequired();
        });

        builder.Property(o => o.CouponCode).HasMaxLength(50);
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.ShippingCarrier).HasMaxLength(100);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(o => o.DomainEvents);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);

        builder.Property(i => i.Sku)
            .HasConversion(sku => sku.Value, value => new Sku(value))
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsOne(i => i.UnitPrice, p =>
        {
            p.Property(m => m.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2).IsRequired();
            p.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(10).IsRequired();
        });
    }
}

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(h => h.Comment).HasMaxLength(500);
        builder.Property(h => h.ChangedBy).HasMaxLength(100).IsRequired();
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.TransactionId).HasMaxLength(150);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10).IsRequired();
        });
    }
}

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Value).HasPrecision(18, 2).IsRequired();

        builder.OwnsOne(c => c.MinOrderAmount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("min_order_amount").HasPrecision(18, 2);
            p.Property(m => m.Currency).HasColumnName("min_order_currency").HasMaxLength(10);
        });

        builder.OwnsOne(c => c.MaxDiscountAmount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("max_discount_amount").HasPrecision(18, 2);
            p.Property(m => m.Currency).HasColumnName("max_discount_currency").HasMaxLength(10);
        });
    }
}

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Title).HasMaxLength(150);
        builder.Property(r => r.Comment).IsRequired().HasMaxLength(2000);
        builder.HasIndex(r => r.ProductId);
    }
}

public sealed class InventoryLogConfiguration : IEntityTypeConfiguration<InventoryLog>
{
    public void Configure(EntityTypeBuilder<InventoryLog> builder)
    {
        builder.ToTable("inventory_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Reason).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(l => l.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Note).HasMaxLength(500);
        builder.HasIndex(l => l.VariantId);
    }
}

