using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Features.Admin.Catalog.Commands.CreateCategory;
using ApexCommerce.Application.Features.Admin.Catalog.Commands.CreateProduct;
using ApexCommerce.Application.Features.Admin.Catalog.Commands.UpdateProduct;

namespace ApexCommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Policy = "ManagerOrAdmin")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create new product with variants, attributes, and stock.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/admin/products/{id}", new { id });
    }

    /// <summary>
    /// Update existing product details and pricing.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProductId)
            return BadRequest("Route ID and payload ID mismatch.");

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Create new category in catalog taxonomy.
    /// </summary>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/admin/categories/{id}", new { id });
    }
}

