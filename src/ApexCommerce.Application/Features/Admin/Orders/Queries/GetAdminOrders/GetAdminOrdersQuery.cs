using MediatR;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Application.Features.Admin.Orders.Queries.GetAdminOrders;

public sealed record GetAdminOrdersQuery(
    OrderStatus? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<AdminOrderDto>>;

public interface IAdminOrderQueryRepository
{
    Task<PaginatedList<AdminOrderDto>> GetAdminOrdersAsync(GetAdminOrdersQuery query, CancellationToken cancellationToken);
}

public sealed class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, PaginatedList<AdminOrderDto>>
{
    private readonly IAdminOrderQueryRepository _queryRepository;

    public GetAdminOrdersQueryHandler(IAdminOrderQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PaginatedList<AdminOrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _queryRepository.GetAdminOrdersAsync(request, cancellationToken);
    }
}

