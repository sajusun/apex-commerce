using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Application.Common.DTOs.Store;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description, Guid? ParentCategoryId, int DisplayOrder, List<CategoryDto> SubCategories);
public sealed record BrandDto(Guid Id, string Name, string Slug, string? LogoUrl);

public sealed record ProductVariantDto(
    Guid Id,
    string Sku,
    string VariantName,
    decimal AdditionalPrice,
    decimal EffectivePrice,
    string Currency,
    int AvailableStock,
    bool InStock
);

public sealed record ProductImageDto(Guid Id, string ImageUrl, string AltText, bool IsPrimary);

public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string ShortDescription,
    string CategoryName,
    string BrandName,
    decimal BasePrice,
    string Currency,
    int TotalStock,
    string? PrimaryImageUrl,
    bool IsFeatured
);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    Guid CategoryId,
    string CategoryName,
    Guid BrandId,
    string BrandName,
    decimal BasePrice,
    string Currency,
    List<ProductVariantDto> Variants,
    List<ProductImageDto> Images,
    List<ReviewDto> Reviews
);

public sealed record CartItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal TotalPrice,
    int AvailableStock
);

public sealed record CartDto(
    Guid Id,
    decimal SubTotal,
    string Currency,
    int TotalItems,
    List<CartItemDto> Items
);

public sealed record OrderItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal TotalPrice
);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingCost,
    decimal TotalAmount,
    string Currency,
    string? CouponCode,
    string ShippingAddress,
    string? TrackingNumber,
    string? ShippingCarrier,
    DateTime CreatedAtUtc,
    List<OrderItemDto> Items
);

public sealed record ReviewDto(
    Guid Id,
    string CustomerName,
    int Rating,
    string Title,
    string Comment,
    bool IsVerifiedPurchase,
    DateTime CreatedAtUtc
);

