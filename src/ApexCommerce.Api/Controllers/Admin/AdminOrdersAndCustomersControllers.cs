using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Application.Features.Admin.Customers.Commands.UpdateCustomerStatus;
using ApexCommerce.Application.Features.Admin.Customers.Queries.GetAdminCustomers;
using ApexCommerce.Application.Features.Admin.Orders.Commands.RefundOrder;
using ApexCommerce.Application.Features.Admin.Orders.Commands.UpdateOrderStatus;
using ApexCommerce.Application.Features.Admin.Orders.Queries.GetAdminOrders;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Policy = "ManagerOrAdmin")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminOrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve filterable order management list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AdminOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] GetAdminOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    public sealed record UpdateStatusRequestBody(OrderStatus Status, string? TrackingNumber, string? Carrier);

    /// <summary>
    /// Update order fulfillment status (e.g. Shipped with tracking or Delivered).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequestBody body, CancellationToken cancellationToken)
    {
        var operatorName = _currentUserService.Email ?? "Admin";
        var command = new UpdateOrderStatusCommand(id, body.Status, body.TrackingNumber, body.Carrier, operatorName);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    public sealed record RefundRequestBody(string Reason, bool RestockItems = true);

    /// <summary>
    /// Process order refund and restock items back into inventory.
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefundOrder(Guid id, [FromBody] RefundRequestBody body, CancellationToken cancellationToken)
    {
        var operatorName = _currentUserService.Email ?? "Admin";
        var command = new RefundOrderCommand(id, body.Reason, operatorName, body.RestockItems);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/admin/customers")]
[Authorize(Policy = "ManagerOrAdmin")]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve registered customers list with order count and lifetime value (LTV).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AdminCustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers([FromQuery] GetAdminCustomersQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    public sealed record UpdateStatusRequestBody(bool IsActive);

    /// <summary>
    /// Block or unblock a customer account.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCustomerStatus(Guid id, [FromBody] UpdateStatusRequestBody body, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateCustomerStatusCommand(id, body.IsActive), cancellationToken);
        return NoContent();
    }
}

