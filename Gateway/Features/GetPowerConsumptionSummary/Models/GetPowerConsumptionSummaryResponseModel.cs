namespace Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;

public class GetPowerConsumptionSummaryResponseModel
{
    public required string Room { get; set; }

    public required double Energy { get; set; }
}
