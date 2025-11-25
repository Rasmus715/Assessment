using Assessment.Gateway.Features.GetAirQualitySummary;
using Assessment.Gateway.Features.GetAirQualitySummary.Models;
using Assessment.Gateway.Features.GetLatestMetrics;
using Assessment.Gateway.Features.GetLatestMetrics.Models;
using Assessment.Gateway.Features.GetLatestMotionData;
using Assessment.Gateway.Features.GetMetrics;
using Assessment.Gateway.Features.GetMetricsMetadata;
using Assessment.Gateway.Features.GetMotionLatestData.Models;
using Assessment.Gateway.Features.GetPowerConsumptionSummary;
using Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;

namespace Assessment.Gateway.Services.Abstraction;

public interface IInfluxDbService
{
    Task<GetMetricsMetadataResponse> GetMetricsMetadataAsync(
        GetMetricsMetadataQuery request,
        CancellationToken cancellationToken);

    Task<List<ISensorEvent>> GetMetricsAsync(
        GetMetricsQuery request,
        CancellationToken cancellationToken = default);

    Task<List<GetLatestMotionDataResponseModel>> GetLatestMotionDataAsync(
        GetLatestMotionDataQuery query,
        CancellationToken cancellationToken);

    Task<List<GetLatestMetricsResponseModel>> GetLatestMetricsAsync(
        GetLatestMetricsQuery query,
        CancellationToken cancellationToken);

    Task<List<GetAirQualitySummaryResponseModel>> GetAirQualitySummaryAsync(
        GetAirQualitySummaryQuery query,
        CancellationToken cancellationToken);

    Task<List<GetPowerConsumptionSummaryResponseModel>> GetPowerConsumptionSummaryAsync(
        GetPowerConsumptionSummaryQuery query,
        CancellationToken cancellationToken);
}
