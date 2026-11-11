using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Admin.Analytics.Queries.GetDashboardOverview;
using ApexCommerce.Application.Features.Admin.Customers.Queries.GetAdminCustomers;
using ApexCommerce.Application.Features.Admin.Inventory.Commands.AdjustStock;
using ApexCommerce.Application.Features.Admin.Inventory.Queries.GetLowStockVariants;
using ApexCommerce.Application.Features.Admin.Orders.Queries.GetAdminOrders;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetProducts;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Infrastructure.BackgroundJobs;
using ApexCommerce.Infrastructure.Caching;
using ApexCommerce.Infrastructure.Identity;
using ApexCommerce.Infrastructure.Persistence;
using ApexCommerce.Infrastructure.Persistence.Repositories;

namespace ApexCommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Connection (MySQL + EF Core)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=127.0.0.1;Port=3306;Database=apexcommerce_db;Uid=sakhawat;Pwd=;";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySQL(connectionString);
        });

        // 2. Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // 3. Repositories
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(sp => sp.GetRequiredService<ProductRepository>());
        services.AddScoped<IProductQueryRepository>(sp => sp.GetRequiredService<ProductRepository>());

        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<CartRepository>();
        services.AddScoped<ICartRepository>(sp => sp.GetRequiredService<CartRepository>());
        services.AddScoped<ICartQueryService>(sp => sp.GetRequiredService<CartRepository>());

        services.AddScoped<OrderRepository>();
        services.AddScoped<IOrderRepository>(sp => sp.GetRequiredService<OrderRepository>());
        services.AddScoped<IAdminOrderQueryRepository>(sp => sp.GetRequiredService<OrderRepository>());

        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());
        services.AddScoped<IAdminCustomerQueryRepository>(sp => sp.GetRequiredService<UserRepository>());

        services.AddScoped<ICouponRepository, CouponRepository>();

        services.AddScoped<InventoryLogRepository>();
        services.AddScoped<IInventoryLogRepository>(sp => sp.GetRequiredService<InventoryLogRepository>());
        services.AddScoped<IInventoryQueryRepository>(sp => sp.GetRequiredService<InventoryLogRepository>());

        services.AddScoped<IAdminAnalyticsQueryRepository, AdminAnalyticsQueryRepository>();

        // 4. Redis Distributed Cache
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "ApexCommerce:";
        });
        services.AddSingleton<ICacheService, DistributedCacheService>();

        // 5. Identity & Security
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 6. Channel Background Task Queue & Worker
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<QueuedHostedService>();

        return services;
    }
}

