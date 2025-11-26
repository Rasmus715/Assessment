using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetMetricsMetadata;

public class GetMetricsMetadataQueryHandler(IInfluxDbService influx)
    : IQueryHandler<GetMetricsMetadataQuery, GetMetricsMetadataResponse>
{
    public async ValueTask<GetMetricsMetadataResponse> Handle(
        GetMetricsMetadataQuery query,
        CancellationToken cancellationToken)
    {
        return await influx.GetMetricsMetadataAsync(query, cancellationToken);
    }
}