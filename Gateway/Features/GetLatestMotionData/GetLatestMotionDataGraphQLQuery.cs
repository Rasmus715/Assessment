using Assessment.Gateway.Features.GetMotionLatestData.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMotionData;

[ExtendObjectType("Query")]
public class GetLatestMotionDataGraphQLQuery
{
    public async Task<IEnumerable<GetLatestMotionDataResponseModel>> GetLatestMotionData(
        [Service] IMediator mediator,
        DateTime? timestamp,
        bool useLatestValue = false)
    {
        var query = new GetLatestMotionDataQuery()
        {
            Timestamp = timestamp,
            UseLatestValue = useLatestValue
        };

        return await mediator.Send(query);
    }
}