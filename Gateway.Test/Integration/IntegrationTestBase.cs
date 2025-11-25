using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace Assessment.Gateway.Tests.Integration;

public class IntegrationTestBase
{
    protected InfluxDBClient CreateInfluxClient()
    {
        return new InfluxDBClient(InfluxConfiguration.InfluxUrl, "admin", "admin123");
    }

    protected async Task WriteTestData(string measurement, string room, Dictionary<string, object> fields, DateTime timestamp)
    {
        using var client = CreateInfluxClient();
        var writeApi = client.GetWriteApiAsync();

        var point = PointData
            .Measurement(measurement)
            .Tag("name", room)
            .Timestamp(timestamp, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

        foreach (var field in fields)
        {
            if (field.Value is int intValue)
                point = point.Field(field.Key, intValue);
            else if (field.Value is double doubleValue)
                point = point.Field(field.Key, doubleValue);
            else if (field.Value is float floatValue)
                point = point.Field(field.Key, floatValue);
            else if (field.Value is bool boolValue)
                point = point.Field(field.Key, boolValue);
            else
                point = point.Field(field.Key, field.Value.ToString() ?? "");
        }

        await writeApi.WritePointAsync(point, InfluxConfiguration.InfluxBucket, InfluxConfiguration.InfluxOrg);

        // Даем время на запись
        await Task.Delay(5000);
    }

    protected async Task ClearBucket()
    {
        using var client = CreateInfluxClient();

        var start = new DateTime(year: 1900, 1, 1).ToUniversalTime();
        var stop = new DateTime(year: 2260, 1, 1).ToUniversalTime();

        await client.GetDeleteApi().Delete(
            start,
            stop,
            "",
            InfluxConfiguration.InfluxBucket,
            InfluxConfiguration.InfluxOrg
        );
    }
}
