using Assessment.Gateway.Features.GetLatestMetrics.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMetrics;

[ExtendObjectType("Query")]
public class GetLatestMetricsGraphQLQuery
{
    public async Task<IEnumerable<GetLatestMetricsResponseModel>> GetLatestTelemetryAsync(
        [Service] IMediator mediator,
        DateTime? timestamp,
        bool useLatestValue = false)
    {
        var query = new GetLatestMetricsQuery()
        {
            Timestamp = timestamp,
            UseLatestValue = useLatestValue
        };

        return await mediator.Send(query);
    }
}
