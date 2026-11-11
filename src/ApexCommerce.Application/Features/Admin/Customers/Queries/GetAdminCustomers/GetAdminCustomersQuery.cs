using MediatR;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Common.Models;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Customers.Queries.GetAdminCustomers;

public sealed record GetAdminCustomersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 15
) : IRequest<PaginatedList<AdminCustomerDto>>;

public interface IAdminCustomerQueryRepository
{
    Task<PaginatedList<AdminCustomerDto>> GetCustomersAsync(GetAdminCustomersQuery query, CancellationToken cancellationToken);
}

public sealed class GetAdminCustomersQueryHandler : IRequestHandler<GetAdminCustomersQuery, PaginatedList<AdminCustomerDto>>
{
    private readonly IAdminCustomerQueryRepository _queryRepository;

    public GetAdminCustomersQueryHandler(IAdminCustomerQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PaginatedList<AdminCustomerDto>> Handle(GetAdminCustomersQuery request, CancellationToken cancellationToken)
    {
        return await _queryRepository.GetCustomersAsync(request, cancellationToken);
    }
}

