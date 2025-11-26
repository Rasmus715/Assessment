using Assessment.Gateway.Features.GetAirQualitySummary.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetAirQualitySummary;

public record GetAirQualitySummaryQuery : IRequest<List<GetAirQualitySummaryResponseModel>>
{
    public DateTime? Timestamp { get; set; }

    public bool UseLatestValue { get; set; }
}