using Assessment.Gateway.Features.GetLatestMetrics.Models;
using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMetrics;

public record GetLatestMetricsQueryHandler(IInfluxDbService influxDbService)
    : IRequestHandler<GetLatestMetricsQuery, List<GetLatestMetricsResponseModel>>
{
    public async ValueTask<List<GetLatestMetricsResponseModel>> Handle(
        GetLatestMetricsQuery request,
        CancellationToken cancellationToken)
    {
        return await influxDbService.GetLatestMetricsAsync(request, cancellationToken);
    }
}
