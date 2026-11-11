using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetProducts;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository, IProductQueryRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), cancellationToken);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task<PaginatedList<ProductSummaryDto>> SearchProductsAsync(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            dbQuery = dbQuery.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.BrandId.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.BrandId == query.BrandId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.BasePrice.Amount >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.BasePrice.Amount <= query.MaxPrice.Value);
        }

        if (query.IsFeatured.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.IsFeatured == query.IsFeatured.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Slug,
                p.ShortDescription,
                p.Category != null ? p.Category.Name : string.Empty,
                p.Brand != null ? p.Brand.Name : string.Empty,
                p.BasePrice.Amount,
                p.BasePrice.Currency,
                p.Variants.Sum(v => v.AvailableStock),
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                p.IsFeatured
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedList<ProductSummaryDto>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<ProductDetailDto?> GetProductDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant() && p.IsActive, cancellationToken);

        if (product == null) return null;

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .Select(r => new ReviewDto(
                r.Id,
                r.CustomerName,
                r.Rating,
                r.Title,
                r.Comment,
                r.IsVerifiedPurchase,
                r.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.BrandId,
            product.Brand?.Name ?? string.Empty,
            product.BasePrice.Amount,
            product.BasePrice.Currency,
            product.Variants.Select(v => new ProductVariantDto(
                v.Id,
                v.Sku.Value,
                v.VariantName,
                v.AdditionalPrice.Amount,
                product.BasePrice.Amount + v.AdditionalPrice.Amount,
                product.BasePrice.Currency,
                v.AvailableStock,
                v.AvailableStock > 0
            )).ToList(),
            product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto(
                i.Id,
                i.ImageUrl,
                i.AltText,
                i.IsPrimary
            )).ToList(),
            reviews
        );
    }
}

