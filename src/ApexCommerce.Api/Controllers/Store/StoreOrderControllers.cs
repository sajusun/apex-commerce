using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Store.Checkout.Commands.CheckoutOrder;
using ApexCommerce.Application.Features.Store.Orders.Commands.CancelOrder;
using ApexCommerce.Application.Features.Store.Orders.Queries.GetCustomerOrders;
using ApexCommerce.Application.Features.Store.Orders.Queries.GetOrderById;

namespace ApexCommerce.Api.Controllers.Store;

[ApiController]
[Route("api/v1/store/checkout")]
[Authorize]
public sealed class StoreCheckoutController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public StoreCheckoutController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public sealed record CheckoutRequestBody(
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        string? CouponCode = null
    );

    /// <summary>
    /// Checkout active shopping cart: reserves stock, applies promo discounts, and creates order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestBody body, CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.UserId!.Value;
        var customerEmail = _currentUserService.Email!;

        var command = new CheckoutOrderCommand(
            customerId,
            customerEmail,
            body.Street,
            body.City,
            body.State,
            body.PostalCode,
            body.Country,
            body.CouponCode
        );

        var order = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(StoreOrdersController.GetOrderById), "StoreOrders", new { id = order.Id }, order);
    }
}

[ApiController]
[Route("api/v1/store/orders")]
[Authorize]
public sealed class StoreOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public StoreOrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve customer's historical orders list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerOrders(CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.UserId!.Value;
        var orders = await _mediator.Send(new GetCustomerOrdersQuery(customerId), cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Retrieve specific order details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.UserId!.Value;
        var order = await _mediator.Send(new GetOrderByIdQuery(id, customerId), cancellationToken);
        return Ok(order);
    }

    public sealed record CancelRequestBody(string Reason);

    /// <summary>
    /// Cancel unfulfilled order and release inventory stock back to warehouse.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelRequestBody body, CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.UserId!.Value;
        await _mediator.Send(new CancelOrderCommand(id, customerId, body.Reason), cancellationToken);
        return NoContent();
    }
}

