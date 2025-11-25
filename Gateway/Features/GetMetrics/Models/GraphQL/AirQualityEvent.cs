using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;

namespace Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;

public class AirQualityEvent : ISensorEvent
{
    public string Type => "air_quality";

    public required string Room { get; set; }

    public required DateTime Time { get; set; }

    public required int Co2 { get; set; }

    public required int Pm25 { get; set; }

    public required int Humidity { get; set; }
}