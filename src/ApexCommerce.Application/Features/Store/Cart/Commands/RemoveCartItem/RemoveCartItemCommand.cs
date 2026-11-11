using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Cart.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(
    Guid? CustomerId,
    string? SessionId,
    Guid CartItemId
) : IRequest<CartDto>;

public sealed class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCartItemCommandHandler(
        ICartRepository cartRepository,
        ICartQueryService cartQueryService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _cartQueryService = cartQueryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
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

        if (cart != null)
        {
            cart.RemoveItem(request.CartItemId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await _cartQueryService.MapCartToDtoAsync(cart, cancellationToken);
        }

        return new CartDto(Guid.Empty, 0m, "USD", 0, new List<CartItemDto>());
    }
}

