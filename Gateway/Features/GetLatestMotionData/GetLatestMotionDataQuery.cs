using Assessment.Gateway.Features.GetMotionLatestData.Models;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMotionData;

public class GetLatestMotionDataQuery : IRequest<List<GetLatestMotionDataResponseModel>>
{
    public DateTime? Timestamp { get; set; }

    public bool UseLatestValue { get; set; }
}