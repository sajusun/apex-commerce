using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetCategories;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetProductBySlug;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetProducts;

namespace ApexCommerce.Api.Controllers.Store;

[ApiController]
[Route("api/v1/store/products")]
[AllowAnonymous]
public sealed class StoreProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve paginated product catalog with filtering, full-text search, and Redis caching.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve full product details including variants, image gallery, and verified reviews.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/store/categories")]
[AllowAnonymous]
public sealed class StoreCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve complete hierarchical category tree (cached in Redis).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(result);
    }
}

