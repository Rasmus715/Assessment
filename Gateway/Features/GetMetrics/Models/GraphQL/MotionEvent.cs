using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;

namespace Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;

public record MotionEvent : ISensorEvent
{
    public string Type => "motion";

    public required string Room { get; set; }

    public required DateTime Time { get; set; }

    public required bool MotionDetected { get; set; }
}