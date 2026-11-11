using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Catalog.Queries.GetProducts;

public sealed record GetProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsFeatured = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PaginatedList<ProductSummaryDto>>;

public interface IProductQueryRepository
{
    Task<PaginatedList<ProductSummaryDto>> SearchProductsAsync(GetProductsQuery query, CancellationToken cancellationToken);
    Task<ProductDetailDto?> GetProductDetailBySlugAsync(string slug, CancellationToken cancellationToken);
}

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedList<ProductSummaryDto>>
{
    private readonly IProductQueryRepository _queryRepository;
    private readonly ICacheService _cacheService;

    public GetProductsQueryHandler(IProductQueryRepository queryRepository, ICacheService cacheService)
    {
        _queryRepository = queryRepository;
        _cacheService = cacheService;
    }

    public async Task<PaginatedList<ProductSummaryDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Cache general storefront first-page catalog
        if (string.IsNullOrWhiteSpace(request.Search) && !request.CategoryId.HasValue && !request.BrandId.HasValue && request.Page == 1)
        {
            var cacheKey = "store:products:featured:p1";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                () => _queryRepository.SearchProductsAsync(request, cancellationToken),
                TimeSpan.FromMinutes(5),
                cancellationToken);
        }

        return await _queryRepository.SearchProductsAsync(request, cancellationToken);
    }
}

