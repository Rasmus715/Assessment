using Assessment.Gateway.Features.GetAirQualitySummary.Models;
using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetAirQualitySummary;

public class GetAirQualitySummaryQueryHandler(IInfluxDbService influxDbService)
    : IRequestHandler<GetAirQualitySummaryQuery, List<GetAirQualitySummaryResponseModel>>
{
    public async ValueTask<List<GetAirQualitySummaryResponseModel>> Handle(
        GetAirQualitySummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await influxDbService.GetAirQualitySummaryAsync(request, cancellationToken);
    }
}
