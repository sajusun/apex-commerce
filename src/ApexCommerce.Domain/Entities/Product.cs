using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Events;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.Entities;

public sealed class Product : AggregateRoot<Guid>
{
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public Guid BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public uint Version { get; private set; } // Optimistic Concurrency Token

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public int TotalAvailableStock => _variants.Sum(v => v.AvailableStock);

    private Product() { } // EF Core

    public Product(
        Guid id,
        string name,
        string slug,
        string shortDescription,
        string description,
        Guid categoryId,
        Guid brandId,
        Money basePrice,
        bool isFeatured = false) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Product slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ShortDescription = shortDescription.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        BrandId = brandId;
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        IsFeatured = isFeatured;
        IsActive = true;
    }

    public void Update(
        string name,
        string slug,
        string shortDescription,
        string description,
        Guid categoryId,
        Guid brandId,
        Money basePrice,
        bool isActive,
        bool isFeatured)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ShortDescription = shortDescription.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        BrandId = brandId;
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        IsActive = isActive;
        IsFeatured = isFeatured;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public ProductVariant AddVariant(
        Guid variantId,
        Sku sku,
        string variantName,
        Money additionalPrice,
        int initialStock,
        int lowStockThreshold = 5,
        string? barcode = null)
    {
        if (_variants.Any(v => v.Sku == sku))
            throw new InvalidOperationException($"A variant with SKU '{sku}' already exists on this product.");

        var variant = new ProductVariant(variantId, Id, sku, variantName, additionalPrice, initialStock, lowStockThreshold, barcode);
        _variants.Add(variant);
        UpdatedAtUtc = DateTime.UtcNow;
        return variant;
    }

    public void AddImage(Guid imageId, string imageUrl, string altText, bool isPrimary = false, int displayOrder = 0)
    {
        if (isPrimary)
        {
            foreach (var img in _images) img.ClearPrimary();
        }

        _images.Add(new ProductImage(imageId, Id, imageUrl, altText, isPrimary, displayOrder));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ProductVariant : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string VariantName { get; private set; } = string.Empty; // e.g., "Charcoal / XL"
    public Money AdditionalPrice { get; private set; } = null!;
    public int AvailableStock { get; private set; }
    public int ReservedStock { get; private set; }
    public int LowStockThreshold { get; private set; }
    public string? Barcode { get; private set; }
    public bool IsActive { get; private set; } = true;

    public bool IsLowStock => AvailableStock <= LowStockThreshold;

    private ProductVariant() { } // EF Core

    public ProductVariant(
        Guid id,
        Guid productId,
        Sku sku,
        string variantName,
        Money additionalPrice,
        int initialStock,
        int lowStockThreshold = 5,
        string? barcode = null) : base(id)
    {
        if (initialStock < 0) throw new ArgumentException("Initial stock cannot be negative.", nameof(initialStock));

        ProductId = productId;
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        VariantName = variantName.Trim();
        AdditionalPrice = additionalPrice ?? Money.Zero();
        AvailableStock = initialStock;
        ReservedStock = 0;
        LowStockThreshold = lowStockThreshold;
        Barcode = barcode?.Trim();
        IsActive = true;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reserve must be greater than zero.", nameof(quantity));

        if (AvailableStock < quantity)
            throw new InsufficientStockException(Id, quantity, AvailableStock);

        AvailableStock -= quantity;
        ReservedStock += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to release must be greater than zero.", nameof(quantity));

        if (ReservedStock < quantity)
            throw new InvalidOperationException($"Cannot release {quantity} units; only {ReservedStock} units are reserved.");

        ReservedStock -= quantity;
        AvailableStock += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdjustStock(int quantityDelta, string reason)
    {
        var newStock = AvailableStock + quantityDelta;
        if (newStock < 0)
            throw new InvalidOperationException($"Adjusting stock by {quantityDelta} would result in negative available inventory ({newStock}).");

        AvailableStock = newStock;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ProductImage : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public int DisplayOrder { get; private set; }

    private ProductImage() { } // EF Core

    public ProductImage(Guid id, Guid productId, string imageUrl, string altText, bool isPrimary = false, int displayOrder = 0)
        : base(id)
    {
        ProductId = productId;
        ImageUrl = imageUrl.Trim();
        AltText = altText.Trim();
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    public void ClearPrimary() => IsPrimary = false;
    public void SetPrimary() => IsPrimary = true;
}

