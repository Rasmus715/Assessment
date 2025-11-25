using Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetPowerConsumptionSummary;

[ExtendObjectType("Query")]
public class GetPowerConsumptionSummaryGraphQLQuery
{
    public async Task<IEnumerable<GetPowerConsumptionSummaryResponseModel>> GetPowerConsumptionSummary(
       [Service] IMediator mediator,
       DateTime? from,
       DateTime? to)
    {
        var query = new GetPowerConsumptionSummaryQuery
        {
            From = from,
            To = to
        };

        return await mediator.Send(query);
    }
}