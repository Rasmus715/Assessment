namespace Assessment.Gateway.Features.GetLatestMetrics.Models;

public record GetLatestMetricsResponseModel
{
    public required string Room { get; set; }

    public required Dictionary<string, string> Telemetry { get; set; }
}