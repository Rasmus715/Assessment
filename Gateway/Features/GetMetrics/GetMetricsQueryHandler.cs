using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetMetrics;

public record GetMetricsQueryHandler(IInfluxDbService influxDbService)
    : IRequestHandler<GetMetricsQuery, List<ISensorEvent>>
{
    public async ValueTask<List<ISensorEvent>> Handle(
        GetMetricsQuery request,
        CancellationToken cancellationToken)
    {
        return await influxDbService.GetMetricsAsync(request, cancellationToken);
    }
}
