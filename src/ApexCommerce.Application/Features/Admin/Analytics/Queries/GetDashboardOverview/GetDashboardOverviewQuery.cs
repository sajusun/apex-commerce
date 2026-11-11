using MediatR;
using ApexCommerce.Application.Common.DTOs.Admin;

namespace ApexCommerce.Application.Features.Admin.Analytics.Queries.GetDashboardOverview;

public sealed record GetDashboardOverviewQuery : IRequest<DashboardOverviewDto>;

public interface IAdminAnalyticsQueryRepository
{
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SalesChartPointDto>> GetSalesChartAsync(int days, CancellationToken cancellationToken);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int count, CancellationToken cancellationToken);
}

public sealed class GetDashboardOverviewQueryHandler : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IAdminAnalyticsQueryRepository _queryRepository;

    public GetDashboardOverviewQueryHandler(IAdminAnalyticsQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<DashboardOverviewDto> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        return await _queryRepository.GetDashboardOverviewAsync(cancellationToken);
    }
}

