using Microsoft.EntityFrameworkCore;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
        {
            return; // Already seeded
        }

        // 1. Seed Users
        var adminPassword = passwordHasher.HashPassword("AdminSecret123!");
        var managerPassword = passwordHasher.HashPassword("ManagerSecret123!");
        var customerPassword = passwordHasher.HashPassword("SecretPassword123!");

        var adminUser = new User(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "admin@apexcommerce.com",
            adminPassword,
            "System",
            "Admin",
            UserRole.SuperAdmin,
            "+1-555-0199"
        );

        var managerUser = new User(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "manager@apexcommerce.com",
            managerPassword,
            "Sarah",
            "StoreManager",
            UserRole.StoreManager,
            "+1-555-0188"
        );

        var customerUser = new User(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "alex.engineer@enterprise.com",
            customerPassword,
            "Alex",
            "Vance",
            UserRole.Customer,
            "+1-555-0177"
        );

        await context.Users.AddRangeAsync(adminUser, managerUser, customerUser);

        // 2. Seed Categories
        var catElectronics = new Category(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Electronics", "electronics", "Consumer tech and gadgets", null, 1);
        var catAudio = new Category(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Audio & Headphones", "audio-headphones", "Headphones, earbuds, and speakers", catElectronics.Id, 1);
        var catGaming = new Category(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Gaming Gear", "gaming-gear", "Keyboards, mice, and peripherals", catElectronics.Id, 2);
        var catWearables = new Category(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Smart Wearables", "smart-wearables", "Smartwatches and fitness trackers", catElectronics.Id, 3);

        await context.Categories.AddRangeAsync(catElectronics, catAudio, catGaming, catWearables);

        // 3. Seed Brands
        var brandApex = new Brand(Guid.Parse("20000000-0000-0000-0000-000000000001"), "ApexPro", "apexpro", "https://cdn.apexcommerce.com/brands/apex.png", "https://apexpro.io");
        var brandSony = new Brand(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Sony", "sony", "https://cdn.apexcommerce.com/brands/sony.png", "https://sony.com");
        var brandRazer = new Brand(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Razer", "razer", "https://cdn.apexcommerce.com/brands/razer.png", "https://razer.com");

        await context.Brands.AddRangeAsync(brandApex, brandSony, brandRazer);

        // 4. Seed Products with Variants & Images
        var prodHeadphones = new Product(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            "ApexPro ANC Wireless Headphones",
            "apexpro-anc-wireless-headphones",
            "Premium active noise-cancelling wireless over-ear headphones with 40h battery.",
            "Engineered with custom 45mm drivers, active noise cancellation, ambient transparency mode, and ultra-comfortable memory foam ear cushions.",
            catAudio.Id,
            brandApex.Id,
            new Money(249.99m, "USD"),
            isFeatured: true
        );

        prodHeadphones.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000001"), new Sku("APX-ANC-BLK"), "Matte Black", Money.Zero(), 65, 5, "84920192831");
        prodHeadphones.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000002"), new Sku("APX-ANC-WHT"), "Pearl White", new Money(10.00m, "USD"), 40, 5, "84920192832");
        prodHeadphones.AddImage(Guid.NewGuid(), "https://images.unsplash.com/photo-1505740420928-5e560c06d30e", "Headphones Matte Black", isPrimary: true, 1);

        var prodKeyboard = new Product(
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            "ApexClick RGB Mechanical Keyboard",
            "apexclick-rgb-mechanical-keyboard",
            "Hot-swappable mechanical gaming keyboard with per-key RGB illumination.",
            "Featuring aircraft-grade aluminum top plate, PBT double-shot keycaps, Bluetooth 5.2 / 2.4GHz wireless and USB-C connectivity.",
            catGaming.Id,
            brandRazer.Id,
            new Money(159.00m, "USD"),
            isFeatured: true
        );

        prodKeyboard.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000003"), new Sku("KB-RGB-TAC"), "Tactile Brown Switches", Money.Zero(), 50, 8, "73819283741");
        prodKeyboard.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000004"), new Sku("KB-RGB-LIN"), "Linear Red Switches", Money.Zero(), 4, 10, "73819283742"); // Low stock trigger
        prodKeyboard.AddImage(Guid.NewGuid(), "https://images.unsplash.com/photo-1587829741301-dc798b83add3", "Mechanical Keyboard RGB", isPrimary: true, 1);

        var prodWatch = new Product(
            Guid.Parse("30000000-0000-0000-0000-000000000003"),
            "ApexPulse Smart Watch Series 5",
            "apexpulse-smart-watch-series-5",
            "Advanced health and fitness smartwatch with ECG, SPO2, and GPS tracking.",
            "Always-on Retina display, water resistance up to 50 meters, and integrated health sensors providing 24/7 heart rate and sleep analytics.",
            catWearables.Id,
            brandSony.Id,
            new Money(399.00m, "USD"),
            isFeatured: true
        );

        prodWatch.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000005"), new Sku("WATCH-S5-44"), "Midnight Black (44mm)", Money.Zero(), 30, 5, "91827364511");
        prodWatch.AddVariant(Guid.Parse("40000000-0000-0000-0000-000000000006"), new Sku("WATCH-S5-40"), "Silver Aluminum (40mm)", new Money(-20.00m, "USD"), 25, 5, "91827364512");
        prodWatch.AddImage(Guid.NewGuid(), "https://images.unsplash.com/photo-1523275335684-37898b6baf30", "Smart Watch Black", isPrimary: true, 1);

        await context.Products.AddRangeAsync(prodHeadphones, prodKeyboard, prodWatch);

        // 5. Seed Coupons
        var couponWelcome = new Coupon(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "WELCOME10",
            DiscountType.Percentage,
            10m,
            minOrderAmount: new Money(50.00m, "USD"),
            maxDiscountAmount: new Money(100.00m, "USD"),
            expiryDateUtc: DateTime.UtcNow.AddMonths(6),
            maxUsageCount: 1000
        );

        var couponFlat = new Coupon(
            Guid.Parse("50000000-0000-0000-0000-000000000002"),
            "FLAT50",
            DiscountType.FixedAmount,
            50m,
            minOrderAmount: new Money(200.00m, "USD"),
            expiryDateUtc: DateTime.UtcNow.AddMonths(3),
            maxUsageCount: 200
        );

        await context.Coupons.AddRangeAsync(couponWelcome, couponFlat);

        // 6. Seed Reviews
        var review1 = new Review(
            Guid.NewGuid(),
            prodHeadphones.Id,
            customerUser.Id,
            "Alex Vance",
            5,
            "Best ANC headphones I have ever owned!",
            "Sound stage is extraordinarily wide and noise cancelling rivals headphones at twice this price point.",
            isVerifiedPurchase: true
        );

        await context.Reviews.AddAsync(review1);

        // 7. Seed Sample Historical Order
        var sampleAddress = new Address("123 Tech Boulevard", "Austin", "TX", "78701", "USA");
        var sampleOrder = Order.Create(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            "APX-20261010-1001",
            customerUser.Id,
            customerUser.Email,
            sampleAddress,
            sampleAddress,
            new Money(10.00m, "USD"),
            "WELCOME10",
            new Money(24.99m, "USD")
        );

        sampleOrder.AddItem(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            prodHeadphones.Id,
            "ApexPro ANC Wireless Headphones - Matte Black",
            new Sku("APX-ANC-BLK"),
            new Money(249.99m, "USD"),
            1
        );

        sampleOrder.CompletePlacement();
        sampleOrder.MarkPaymentReceived("ch_3O7WqfLkdIwHu7ix28aJkL4z");
        sampleOrder.MarkAsShipped("TRK-FEDEX-992182", "FedEx Express", "System");

        await context.Orders.AddAsync(sampleOrder);

        await context.SaveChangesAsync();
    }
}

