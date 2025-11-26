namespace Assessment.Gateway.Features.GetAirQualitySummary.Models;

public record GetAirQualitySummaryResponseModel
{
    public required string Room { get; set; }

    public required int Co2 { get; set; }

    public required int Pm25 { get; set; }

    public required int Humidity { get; set; }
}
