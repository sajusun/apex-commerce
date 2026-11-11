using MediatR;
using ApexCommerce.Application.Common.DTOs.Admin;
using ApexCommerce.Application.Features.Admin.Analytics.Queries.GetDashboardOverview;

namespace ApexCommerce.Application.Features.Admin.Analytics.Queries.GetSalesChart;

public sealed record GetSalesChartQuery(int Days = 30) : IRequest<IReadOnlyList<SalesChartPointDto>>;

public sealed class GetSalesChartQueryHandler : IRequestHandler<GetSalesChartQuery, IReadOnlyList<SalesChartPointDto>>
{
    private readonly IAdminAnalyticsQueryRepository _queryRepository;

    public GetSalesChartQueryHandler(IAdminAnalyticsQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IReadOnlyList<SalesChartPointDto>> Handle(GetSalesChartQuery request, CancellationToken cancellationToken)
    {
        return await _queryRepository.GetSalesChartAsync(request.Days, cancellationToken);
    }
}

