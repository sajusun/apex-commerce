using NSubstitute;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Application.Features.Store.Checkout.Commands.CheckoutOrder;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.UnitTests;

public class CheckoutOrderCommandHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICouponRepository _couponRepository = Substitute.For<ICouponRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_WhenCartHasItemsAndStockIsSufficient_ShouldCreateOrderAndClearCart()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), customerId);
        var product = new Product(Guid.NewGuid(), "Apex Wireless Headset", "apex-wireless", "Short", "Desc", Guid.NewGuid(), Guid.NewGuid(), new Money(150m, "USD"));
        var variant = product.AddVariant(Guid.NewGuid(), new Sku("APX-HEAD-01"), "Black", Money.Zero(), initialStock: 10);

        cart.AddOrUpdateItem(variant.Id, 2, new Money(150m, "USD"));

        _cartRepository.GetByCustomerIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(cart);
        _productRepository.GetVariantsByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductVariant> { variant });
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new CheckoutOrderCommandHandler(
            _cartRepository,
            _productRepository,
            _orderRepository,
            _couponRepository,
            _unitOfWork
        );

        var command = new CheckoutOrderCommand(
            customerId,
            "alex@test.com",
            "123 Tech Ave",
            "Seattle",
            "WA",
            "98101",
            "USA"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PendingPayment", result.Status);
        Assert.Equal(8, variant.AvailableStock); // 10 - 2 reserved
        Assert.Empty(cart.Items); // Cart was cleared

        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddToCartCommandValidator_WhenQuantityIsZero_ShouldFailValidation()
    {
        // Arrange
        var validator = new AddToCartCommandValidator();
        var command = new AddToCartCommand(Guid.NewGuid(), null, Guid.NewGuid(), 0);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }
}

