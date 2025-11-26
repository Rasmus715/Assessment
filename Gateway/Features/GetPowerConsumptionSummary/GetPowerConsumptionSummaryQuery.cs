using Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetPowerConsumptionSummary;

public record GetPowerConsumptionSummaryQuery : IRequest<List<GetPowerConsumptionSummaryResponseModel>>
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}
