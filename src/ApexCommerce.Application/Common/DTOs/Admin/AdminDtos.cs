using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Application.Common.DTOs.Admin;

public sealed record DashboardOverviewDto(
    decimal TotalRevenue,
    string Currency,
    int OrdersToday,
    int LowStockVariantsCount,
    decimal AverageOrderValue,
    int TotalCustomers,
    decimal RevenueGrowthPercentage
);

public sealed record SalesChartPointDto(string Date, decimal Revenue, int OrdersCount);

public sealed record TopProductDto(Guid ProductId, string ProductName, string Sku, int UnitsSold, decimal TotalRevenue);

public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    string BrandName,
    decimal BasePrice,
    int TotalStock,
    int VariantCount,
    bool IsActive,
    bool IsFeatured,
    DateTime CreatedAtUtc
);

public sealed record AdminOrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerEmail,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    string Currency,
    int ItemsCount,
    string? TrackingNumber,
    DateTime CreatedAtUtc
);

public sealed record LowStockVariantDto(
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string Sku,
    string VariantName,
    int AvailableStock,
    int ReservedStock,
    int LowStockThreshold
);

public sealed record AdminCustomerDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    UserRole Role,
    bool IsActive,
    int TotalOrders,
    decimal LifetimeValue,
    DateTime CreatedAtUtc
);

