using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Cart.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(
    Guid? CustomerId,
    string? SessionId,
    Guid CartItemId,
    int Quantity
) : IRequest<CartDto>;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCartItemCommandHandler(
        ICartRepository cartRepository,
        ICartQueryService cartQueryService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _cartQueryService = cartQueryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
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
            throw new EntityNotFoundException("Cart", request.CustomerId?.ToString() ?? request.SessionId ?? "Unknown");

        cart.UpdateItemQuantity(request.CartItemId, request.Quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _cartQueryService.MapCartToDtoAsync(cart, cancellationToken);
    }
}

