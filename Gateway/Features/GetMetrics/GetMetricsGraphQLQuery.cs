using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetMetrics;

[ExtendObjectType("Query")]
public class GetMetricsGraphQLQuery
{
    public async Task<IEnumerable<ISensorEvent>> GetMetrics(
       [Service] IMediator mediator,
       string? room,
       string? type,
       DateTime? from,
       DateTime? to,
       int? skip,
       int? take)
    {
        var query = new GetMetricsQuery()
        {
            From = from,
            To = to,
            Room = room,
            Type = type,
            Skip = skip,
            Take = take,
        };

        return await mediator.Send(query);
    }
}