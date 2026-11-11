using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Store.Catalog.Queries.GetProducts;
using ApexCommerce.Domain.Exceptions;

namespace ApexCommerce.Application.Features.Store.Catalog.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto>;

public sealed class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ProductDetailDto>
{
    private readonly IProductQueryRepository _queryRepository;
    private readonly ICacheService _cacheService;

    public GetProductBySlugQueryHandler(IProductQueryRepository queryRepository, ICacheService cacheService)
    {
        _queryRepository = queryRepository;
        _cacheService = cacheService;
    }

    public async Task<ProductDetailDto> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"store:product:slug:{request.Slug.ToLowerInvariant()}";

        var product = await _cacheService.GetOrCreateAsync(
            cacheKey,
            () => _queryRepository.GetProductDetailBySlugAsync(request.Slug, cancellationToken),
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return product ?? throw new EntityNotFoundException("Product", request.Slug);
    }
}

