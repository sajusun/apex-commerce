using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Admin.Analytics.Queries.GetDashboardOverview;
using ApexCommerce.Application.Features.Admin.Analytics.Queries.GetSalesChart;
using ApexCommerce.Application.Features.Admin.Inventory.Commands.AdjustStock;
using ApexCommerce.Application.Features.Admin.Inventory.Queries.GetLowStockVariants;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Policy = "ManagerOrAdmin")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve executive dashboard overview KPIs (Gross revenue, orders count, low-stock items, average order value).
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardOverviewQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve daily sales and revenue chart points for visualization.
    /// </summary>
    [HttpGet("sales-chart")]
    [ProducesResponseType(typeof(IReadOnlyList<SalesChartPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesChart([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSalesChartQuery(days), cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/admin/inventory")]
[Authorize(Policy = "ManagerOrAdmin")]
public sealed class AdminInventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminInventoryController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve list of all product variants below their safety stock threshold.
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockVariantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockVariants(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLowStockVariantsQuery(), cancellationToken);
        return Ok(result);
    }

    public sealed record AdjustStockRequestBody(int QuantityDelta, InventoryChangeReason Reason, string? Note);

    /// <summary>
    /// Adjust inventory stock manually (Restock or write-off) with audit logging.
    /// </summary>
    [HttpPost("{variantId:guid}/adjust")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustStock(Guid variantId, [FromBody] AdjustStockRequestBody body, CancellationToken cancellationToken)
    {
        var operatorName = _currentUserService.Email ?? "Admin";
        var command = new AdjustStockCommand(variantId, body.QuantityDelta, body.Reason, operatorName, body.Note);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

