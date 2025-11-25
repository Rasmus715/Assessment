using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;

namespace Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;

public record EnergyEvent : ISensorEvent
{
    public string Type => "energy";

    public required string Room { get; set; }

    public required DateTime Time { get; set; }

    public required double Energy { get; set; }
}