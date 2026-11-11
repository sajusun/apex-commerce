using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.ShortDescription).HasMaxLength(500);
        builder.Property(p => p.Description).HasMaxLength(4000);

        builder.OwnsOne(p => p.BasePrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("base_price_amount").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("base_price_currency").HasMaxLength(10).IsRequired();
        });

        builder.Property(p => p.Version).IsRowVersion();

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany()
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Sku)
            .HasConversion(sku => sku.Value, value => new Sku(value))
            .HasColumnName("sku")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.VariantName).IsRequired().HasMaxLength(100);

        builder.OwnsOne(v => v.AdditionalPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("additional_price_amount").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("additional_price_currency").HasMaxLength(10).IsRequired();
        });

        builder.Property(v => v.Barcode).HasMaxLength(100);
    }
}

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).HasMaxLength(200);
    }
}

