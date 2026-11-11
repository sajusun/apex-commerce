using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.Entities;

public sealed class Brand : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? Website { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Brand() { } // EF Core

    public Brand(Guid id, string name, string slug, string? logoUrl = null, string? website = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Brand name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Brand slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl?.Trim();
        Website = website?.Trim();
        IsActive = true;
    }

    public void Update(string name, string slug, string? logoUrl, string? website, bool isActive)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl?.Trim();
        Website = website?.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

