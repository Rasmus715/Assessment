namespace Assessment.Gateway.Features.GetMetricsMetadata;

public record GetMetricsMetadataResponse
{
    public List<string> Types { get; set; } = [];

    public List<string> Rooms { get; set; } = [];
}
