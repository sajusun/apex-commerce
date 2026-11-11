using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;

public sealed record AddToCartCommand(
    Guid? CustomerId,
    string? SessionId,
    Guid VariantId,
    int Quantity
) : IRequest<CartDto>;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}

public interface ICartQueryService
{
    Task<CartDto> MapCartToDtoAsync(Domain.Entities.Cart cart, CancellationToken cancellationToken);
}

public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly IUnitOfWork _unitOfWork;

    public AddToCartCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICartQueryService cartQueryService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _cartQueryService = cartQueryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ProductVariant), request.VariantId);

        if (variant.AvailableStock < request.Quantity)
            throw new InsufficientStockException(variant.Id, request.Quantity, variant.AvailableStock);

        var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Product), variant.ProductId);

        var effectiveUnitPrice = product.BasePrice.Add(variant.AdditionalPrice);

        Domain.Entities.Cart? cart = null;
        if (request.CustomerId.HasValue)
        {
            cart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            cart = await _cartRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        }

        if (cart == null)
        {
            cart = new Domain.Entities.Cart(Guid.NewGuid(), request.CustomerId, request.SessionId);
            await _cartRepository.AddAsync(cart, cancellationToken);
        }

        cart.AddOrUpdateItem(variant.Id, request.Quantity, effectiveUnitPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _cartQueryService.MapCartToDtoAsync(cart, cancellationToken);
    }
}

