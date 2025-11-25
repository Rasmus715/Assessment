namespace Assessment.Gateway.Features.GetMotionLatestData.Models;

public class GetLatestMotionDataResponseModel
{
    public required string Room { get; set; }

    public required bool MotionDetected { get; set; }
}