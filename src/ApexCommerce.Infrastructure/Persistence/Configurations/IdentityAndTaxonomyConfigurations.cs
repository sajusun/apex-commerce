using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexCommerce.Domain.Entities;

namespace ApexCommerce.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(u => u.DomainEvents);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);
        builder.HasIndex(r => r.Token);

        builder.Property(r => r.ReplacedByToken).HasMaxLength(500);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.HasOne(c => c.ParentCategory)
            .WithMany(p => p.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);
        builder.Property(b => b.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(b => b.Slug).IsUnique();

        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.Website).HasMaxLength(255);
    }
}

