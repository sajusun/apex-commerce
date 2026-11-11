using ApexCommerce.Domain.Entities;

namespace ApexCommerce.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
}

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
    Task DeleteAsync(Cart cart, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
}

