namespace Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;

public interface ISensorEvent
{
    public string Type { get; }

    public string Room { get; set; }

    public DateTime Time { get; set; }
}