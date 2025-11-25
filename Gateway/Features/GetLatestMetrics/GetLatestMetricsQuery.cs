using Assessment.Gateway.Features.GetLatestMetrics.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMetrics;

public class GetLatestMetricsQuery : IRequest<List<GetLatestMetricsResponseModel>>
{
    public DateTime? Timestamp { get; set; }

    public bool UseLatestValue { get; set; }
}