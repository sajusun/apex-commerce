using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Store.Cart.Commands.AddToCart;
using ApexCommerce.Application.Features.Store.Cart.Commands.ApplyCoupon;
using ApexCommerce.Application.Features.Store.Cart.Commands.RemoveCartItem;
using ApexCommerce.Application.Features.Store.Cart.Commands.UpdateCartItem;
using ApexCommerce.Application.Features.Store.Cart.Queries.GetCart;

namespace ApexCommerce.Api.Controllers.Store;

[ApiController]
[Route("api/v1/store/cart")]
[AllowAnonymous]
public sealed class StoreCartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public StoreCartController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public sealed record AddToCartRequestBody(Guid VariantId, int Quantity, string? SessionId);
    public sealed record UpdateQuantityRequestBody(int Quantity, string? SessionId);
    public sealed record ApplyCouponRequestBody(string CouponCode, decimal CartSubtotal, string Currency = "USD");

    /// <summary>
    /// Retrieve current active shopping cart for authenticated customer or guest session.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart([FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(_currentUserService.UserId, sessionId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add variant item to cart with available stock validation.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequestBody body, CancellationToken cancellationToken)
    {
        var command = new AddToCartCommand(_currentUserService.UserId, body.SessionId, body.VariantId, body.Quantity);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update item quantity in shopping cart.
    /// </summary>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] UpdateQuantityRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateCartItemCommand(_currentUserService.UserId, body.SessionId, id, body.Quantity);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove item from shopping cart.
    /// </summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(Guid id, [FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var command = new RemoveCartItemCommand(_currentUserService.UserId, sessionId, id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Validate and apply promotional discount coupon code.
    /// </summary>
    [HttpPost("apply-coupon")]
    [ProducesResponseType(typeof(CouponValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApplyCouponCommand(body.CouponCode, body.CartSubtotal, body.Currency), cancellationToken);
        return Ok(result);
    }
}

