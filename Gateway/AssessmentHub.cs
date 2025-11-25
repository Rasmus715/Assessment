using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace Assessment.Gateway;

public class AssessmentHub : Hub
{
    public async Task TelemetryReceived(SensorSignalREvent sensorEvent)
    {
        await Clients.All.SendAsync("TelemetryReceived", sensorEvent);
    }
}

public class SensorSignalREvent : ISensorEvent
{
    public required string Type { get; set; }

    public required string Room { get; set; }

    public DateTime Time { get; set; }

    public bool? MotionDetected { get; set; }

    public double? Energy { get; set; }

    public int? Co2 { get; set; }

    public int? Pm25 { get; set; }

    public int? Humidity { get; set; }
}