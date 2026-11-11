using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Store.Cart.Queries.GetCart;

public sealed record GetCartQuery(Guid? CustomerId, string? SessionId) : IRequest<CartDto>;

public sealed class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartQueryService _cartQueryService;

    public GetCartQueryHandler(ICartRepository cartRepository, ICartQueryService cartQueryService)
    {
        _cartRepository = cartRepository;
        _cartQueryService = cartQueryService;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
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
        {
            return new CartDto(Guid.Empty, 0m, "USD", 0, new List<CartItemDto>());
        }

        return await _cartQueryService.MapCartToDtoAsync(cart, cancellationToken);
    }
}

