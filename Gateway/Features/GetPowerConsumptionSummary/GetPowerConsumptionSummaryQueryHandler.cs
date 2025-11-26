using Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;
using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetPowerConsumptionSummary;

public record GetPowerConsumptionSummaryQueryHandler(IInfluxDbService influxDBSerivce)
    : IRequestHandler<GetPowerConsumptionSummaryQuery, List<GetPowerConsumptionSummaryResponseModel>>
{
    public async ValueTask<List<GetPowerConsumptionSummaryResponseModel>> Handle(
        GetPowerConsumptionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await influxDBSerivce.GetPowerConsumptionSummaryAsync(request, cancellationToken);
    }
}
