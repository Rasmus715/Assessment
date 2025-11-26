using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetMetrics;

public record GetMetricsQuery : IRequest<List<ISensorEvent>>
{
    public string? Room { get; set; }

    public string? Type { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int? Skip { get; set; }

    public int? Take { get; set; }
}
