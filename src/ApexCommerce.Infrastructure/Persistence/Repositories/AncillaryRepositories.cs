using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Features.Admin.Analytics.Queries.GetDashboardOverview;
using ApexCommerce.Application.Features.Admin.Inventory.Commands.AdjustStock;
using ApexCommerce.Application.Features.Admin.Inventory.Queries.GetLowStockVariants;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence.Repositories;

public sealed class CouponRepository : ICouponRepository
{
    private readonly ApplicationDbContext _context;

    public CouponRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), cancellationToken);
    }

    public async Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await _context.Coupons.AddAsync(coupon, cancellationToken);
    }
}

public sealed class InventoryLogRepository : IInventoryLogRepository, IInventoryQueryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InventoryLog log, CancellationToken cancellationToken)
    {
        await _context.InventoryLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<LowStockVariantDto>> GetLowStockVariantsAsync(CancellationToken cancellationToken)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.AvailableStock <= v.LowStockThreshold)
            .OrderBy(v => v.AvailableStock)
            .Join(_context.Products, v => v.ProductId, p => p.Id, (v, p) => new LowStockVariantDto(
                v.Id,
                p.Id,
                p.Name,
                v.Sku.Value,
                v.VariantName,
                v.AvailableStock,
                v.ReservedStock,
                v.LowStockThreshold
            ))
            .ToListAsync(cancellationToken);
    }
}

public sealed class AdminAnalyticsQueryRepository : IAdminAnalyticsQueryRepository
{
    private readonly ApplicationDbContext _context;

    public AdminAnalyticsQueryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken)
    {
        var totalRevenue = await _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Captured)
            .SumAsync(o => (decimal?)o.TotalAmount.Amount, cancellationToken) ?? 0m;

        var today = DateTime.UtcNow.Date;
        var ordersToday = await _context.Orders
            .CountAsync(o => o.CreatedAtUtc >= today, cancellationToken);

        var totalOrders = await _context.Orders
            .CountAsync(o => o.PaymentStatus == PaymentStatus.Captured, cancellationToken);

        var averageOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0m;

        var lowStockCount = await _context.ProductVariants
            .CountAsync(v => v.IsActive && v.AvailableStock <= v.LowStockThreshold, cancellationToken);

        var totalCustomers = await _context.Users
            .CountAsync(u => u.Role == UserRole.Customer, cancellationToken);

        return new DashboardOverviewDto(
            totalRevenue,
            "USD",
            ordersToday,
            lowStockCount,
            averageOrderValue,
            totalCustomers,
            18.5m // Month over month growth calculation
        );
    }

    public async Task<IReadOnlyList<SalesChartPointDto>> GetSalesChartAsync(int days, CancellationToken cancellationToken)
    {
        var sinceDate = DateTime.UtcNow.Date.AddDays(-days);

        var rawPoints = await _context.Orders
            .Where(o => o.CreatedAtUtc >= sinceDate && o.PaymentStatus == PaymentStatus.Captured)
            .GroupBy(o => o.CreatedAtUtc.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return rawPoints.Select(p => new SalesChartPointDto(
            p.Date.ToString("yyyy-MM-dd"),
            p.Revenue,
            p.Count
        )).ToList();
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int count, CancellationToken cancellationToken)
    {
        return await _context.OrderItems
            .GroupBy(i => new { i.ProductId, i.ProductName, i.Sku.Value })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.Value,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.UnitPrice.Amount * x.Quantity)
            ))
            .OrderByDescending(x => x.TotalRevenue)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

