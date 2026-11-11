using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Application.Features.Admin.Customers.Queries.GetAdminCustomers;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository, IAdminCustomerQueryRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<PaginatedList<AdminCustomerDto>> GetCustomersAsync(GetAdminCustomersQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            dbQuery = dbQuery.Where(u => u.Email.ToLower().Contains(search) || u.FirstName.ToLower().Contains(search) || u.LastName.ToLower().Contains(search));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var users = await dbQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        // Calculate order counts and lifetime values
        var customerStats = await _context.Orders
            .AsNoTracking()
            .Where(o => userIds.Contains(o.CustomerId) && o.PaymentStatus == Domain.Enums.PaymentStatus.Captured)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalOrders = g.Count(),
                LifetimeValue = g.Sum(x => x.TotalAmount.Amount)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        var dtos = users.Select(u =>
        {
            customerStats.TryGetValue(u.Id, out var stats);
            return new AdminCustomerDto(
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Role,
                u.IsActive,
                stats?.TotalOrders ?? 0,
                stats?.LifetimeValue ?? 0m,
                u.CreatedAtUtc
            );
        }).ToList();

        return new PaginatedList<AdminCustomerDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}

