using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Application.Features.Admin.Orders.Queries.GetAdminOrders;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository, IAdminOrderQueryRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim(), cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<PaginatedList<AdminOrderDto>> GetAdminOrdersAsync(GetAdminOrdersQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.Status == query.Status.Value);
        }

        if (query.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.CreatedAtUtc >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.CreatedAtUtc <= query.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            dbQuery = dbQuery.Where(o => o.OrderNumber.ToLower().Contains(search) || o.CustomerEmail.ToLower().Contains(search));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new AdminOrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerEmail,
                o.Status.ToString(),
                o.PaymentStatus.ToString(),
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.Items.Count,
                o.TrackingNumber,
                o.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedList<AdminOrderDto>(items, totalCount, query.Page, query.PageSize);
    }
}

