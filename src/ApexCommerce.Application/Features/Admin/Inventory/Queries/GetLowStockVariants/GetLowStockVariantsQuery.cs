using MediatR;
using ApexCommerce.Application.Common.DTOs.Admin;

namespace ApexCommerce.Application.Features.Admin.Inventory.Queries.GetLowStockVariants;

public sealed record GetLowStockVariantsQuery : IRequest<IReadOnlyList<LowStockVariantDto>>;

public interface IInventoryQueryRepository
{
    Task<IReadOnlyList<LowStockVariantDto>> GetLowStockVariantsAsync(CancellationToken cancellationToken);
}

public sealed class GetLowStockVariantsQueryHandler : IRequestHandler<GetLowStockVariantsQuery, IReadOnlyList<LowStockVariantDto>>
{
    private readonly IInventoryQueryRepository _queryRepository;

    public GetLowStockVariantsQueryHandler(IInventoryQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IReadOnlyList<LowStockVariantDto>> Handle(GetLowStockVariantsQuery request, CancellationToken cancellationToken)
    {
        return await _queryRepository.GetLowStockVariantsAsync(cancellationToken);
    }
}

