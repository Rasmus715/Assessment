using Assessment.Gateway.Features.GetAirQualitySummary.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetAirQualitySummary;

[ExtendObjectType("Query")]
public class GetAirQualitySummaryGraphQLQuery
{
    public async Task<IEnumerable<GetAirQualitySummaryResponseModel>> GetAirQualitySummaryAsync(
        [Service] IMediator mediator,
        DateTime? timestamp,
        bool useLatestValue)
    {
        var query = new GetAirQualitySummaryQuery()
        {
            Timestamp = timestamp,
            UseLatestValue = useLatestValue,
        };

        return await mediator.Send(query);
    }
}