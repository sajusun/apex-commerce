using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence.Repositories;

public sealed class CartRepository : ICartRepository, ICartQueryService
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public Task DeleteAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _context.Carts.Remove(cart);
        return Task.CompletedTask;
    }

    public async Task<CartDto> MapCartToDtoAsync(Cart cart, CancellationToken cancellationToken)
    {
        var variantIds = cart.Items.Select(i => i.VariantId).ToList();

        var variants = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.ProductId)
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var productIds = variants.Values.Select(v => v.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var itemDtos = new List<CartItemDto>();
        foreach (var item in cart.Items)
        {
            variants.TryGetValue(item.VariantId, out var variant);
            products.TryGetValue(variant?.ProductId ?? Guid.Empty, out var product);

            var productName = product?.Name ?? "Product";
            var variantName = variant?.VariantName ?? "Default";
            var sku = variant?.Sku.Value ?? "SKU";
            var availableStock = variant?.AvailableStock ?? 0;

            itemDtos.Add(new CartItemDto(
                item.Id,
                item.VariantId,
                productName,
                variantName,
                sku,
                item.UnitPrice.Amount,
                item.UnitPrice.Currency,
                item.Quantity,
                item.TotalPrice.Amount,
                availableStock
            ));
        }

        var currency = itemDtos.FirstOrDefault()?.Currency ?? "USD";
        var subtotal = itemDtos.Sum(i => i.TotalPrice);

        return new CartDto(cart.Id, subtotal, currency, itemDtos.Sum(i => i.Quantity), itemDtos);
    }
}

