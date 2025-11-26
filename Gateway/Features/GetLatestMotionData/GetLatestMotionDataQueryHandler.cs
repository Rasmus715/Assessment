using Assessment.Gateway.Features.GetMotionLatestData.Models;
using Assessment.Gateway.Services.Abstraction;
using Mediator;

namespace Assessment.Gateway.Features.GetLatestMotionData;

public record GetLatestMotionDataQueryHandler(IInfluxDbService influxDbService)
    : IRequestHandler<GetLatestMotionDataQuery, List<GetLatestMotionDataResponseModel>>
{
    public async ValueTask<List<GetLatestMotionDataResponseModel>> Handle(
        GetLatestMotionDataQuery request,
        CancellationToken cancellationToken)
    {
        return await influxDbService.GetLatestMotionDataAsync(request, cancellationToken);
    }
}