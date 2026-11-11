using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Exceptions;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Store.Checkout.Commands.CheckoutOrder;

public sealed record CheckoutOrderCommand(
    Guid CustomerId,
    string CustomerEmail,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? CouponCode = null
) : IRequest<OrderDto>;

public sealed class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
{
    public CheckoutOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, OrderDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CheckoutOrderCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Cart
        var cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Active cart not found for checkout.");

        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Cannot checkout with an empty cart.");

        // 2. Fetch all required Product Variants in batch
        var variantIds = cart.Items.Select(i => i.VariantId).Distinct().ToList();
        var variants = (await _productRepository.GetVariantsByIdsAsync(variantIds, cancellationToken))
            .ToDictionary(v => v.Id);

        // 3. Create Shipping Address
        var shippingAddress = new Address(
            request.Street, request.City, request.State, request.PostalCode, request.Country);

        // 4. Calculate Subtotal & Discount
        var subtotalAmount = cart.SubTotalAmount;
        var subtotalMoney = new Money(subtotalAmount, "USD");
        Money discountMoney = Money.Zero();

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken);
            if (coupon != null)
            {
                discountMoney = coupon.CalculateDiscount(subtotalMoney);
                coupon.IncrementUsage();
            }
        }

        // Standard flat shipping: $10.00
        var shippingCost = new Money(10.00m, "USD");

        // 5. Generate Order Number
        var orderNumber = $"APX-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        // 6. Create Order Aggregate
        var order = Order.Create(
            Guid.NewGuid(),
            orderNumber,
            request.CustomerId,
            request.CustomerEmail,
            shippingAddress,
            null,
            shippingCost,
            request.CouponCode,
            discountMoney
        );

        // 7. Reserve inventory stock and add items
        foreach (var item in cart.Items)
        {
            if (!variants.TryGetValue(item.VariantId, out var variant))
                throw new EntityNotFoundException(nameof(ProductVariant), item.VariantId);

            var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Product), variant.ProductId);

            // Reserve stock (throws InsufficientStockException if not enough)
            variant.ReserveStock(item.Quantity);

            var itemPrice = product.BasePrice.Add(variant.AdditionalPrice);
            order.AddItem(variant.Id, product.Id, $"{product.Name} - {variant.VariantName}", variant.Sku, itemPrice, item.Quantity);
        }

        // Complete placement (raises OrderPlacedDomainEvent)
        order.CompletePlacement();

        // 8. Clear Cart & Persist
        cart.Clear();
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Return projected DTO
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.SubTotal.Amount,
            order.DiscountAmount.Amount,
            order.TaxAmount.Amount,
            order.ShippingCost.Amount,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CouponCode,
            order.ShippingAddress.ToString(),
            order.TrackingNumber,
            order.ShippingCarrier,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.VariantId,
                i.ProductName,
                i.Sku.Value,
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.Quantity,
                i.TotalPrice.Amount
            )).ToList()
        );
    }
}

