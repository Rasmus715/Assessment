using Mediator;

namespace Assessment.Gateway.Features.GetMetricsMetadata;

public class GetMetricsMetadataQuery : IQuery<GetMetricsMetadataResponse>
{
    public string? Room { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string? MetricType { get; set; }
}