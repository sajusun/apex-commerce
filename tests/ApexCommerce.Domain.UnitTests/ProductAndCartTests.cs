using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.UnitTests;

public class ProductAndCartTests
{
    [Fact]
    public void ReserveStock_WhenAvailableStockIsSufficient_ShouldDeductStockAndIncreaseReserved()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Test Product", "test-product", "Short", "Desc", Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"));
        var variant = product.AddVariant(Guid.NewGuid(), new Sku("TEST-SKU-01"), "Default", Money.Zero(), initialStock: 20);

        // Act
        variant.ReserveStock(5);

        // Assert
        Assert.Equal(15, variant.AvailableStock);
        Assert.Equal(5, variant.ReservedStock);
    }

    [Fact]
    public void ReserveStock_WhenRequestedExceedsAvailable_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Test Product", "test-product", "Short", "Desc", Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"));
        var variant = product.AddVariant(Guid.NewGuid(), new Sku("TEST-SKU-02"), "Default", Money.Zero(), initialStock: 5);

        // Act & Assert
        Assert.Throws<InsufficientStockException>(() => variant.ReserveStock(10));
    }

    [Fact]
    public void ReleaseStock_ShouldRestoreAvailableInventory()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Test Product", "test-product", "Short", "Desc", Guid.NewGuid(), Guid.NewGuid(), new Money(100m, "USD"));
        var variant = product.AddVariant(Guid.NewGuid(), new Sku("TEST-SKU-03"), "Default", Money.Zero(), initialStock: 10);
        variant.ReserveStock(4);

        // Act
        variant.ReleaseStock(4);

        // Assert
        Assert.Equal(10, variant.AvailableStock);
        Assert.Equal(0, variant.ReservedStock);
    }

    [Fact]
    public void Coupon_CalculateDiscount_WhenPercentage_ShouldDeductCorrectPercentage()
    {
        // Arrange: 20% off with min $50 spend
        var coupon = new Coupon(Guid.NewGuid(), "SAVE20", DiscountType.Percentage, 20m, minOrderAmount: new Money(50m, "USD"));
        var orderSubtotal = new Money(100m, "USD");

        // Act
        var discount = coupon.CalculateDiscount(orderSubtotal);

        // Assert
        Assert.Equal(20m, discount.Amount);
    }

    [Fact]
    public void Coupon_CalculateDiscount_WhenBelowMinimumSpend_ShouldThrowCouponNotApplicableException()
    {
        // Arrange: 10% off with min $100 spend
        var coupon = new Coupon(Guid.NewGuid(), "SAVE10", DiscountType.Percentage, 10m, minOrderAmount: new Money(100m, "USD"));
        var orderSubtotal = new Money(40m, "USD");

        // Act & Assert
        Assert.Throws<CouponNotApplicableException>(() => coupon.CalculateDiscount(orderSubtotal));
    }
}

