using System.Text;
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
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Assessment.Gateway.Services.Abstraction;
using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;

namespace Assessment.Gateway.Services;

public class InfluxDbService : IInfluxDbService
{
    public readonly IInfluxDBClient Influx;

    public readonly string Bucket;

    public readonly string Org;

    public InfluxDbService(
        IConfiguration configuration,
        IInfluxDBClient influxDBClient)
    {
        Org = configuration["Influx:Org"] ?? "rasmus";
        Bucket = configuration["Influx:Bucket"] ?? "weakapp_data";
        Influx = influxDBClient;
    }

    public async Task<GetMetricsMetadataResponse> GetMetricsMetadataAsync(
        GetMetricsMetadataQuery request,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var sb = new StringBuilder();

        var startFlux = request.From.HasValue ? request.From.Value.ToString("o") : "0";
        var stopFlux = request.To.HasValue ? request.To.Value.ToString("o") : "now()";

        sb.AppendLine($@"from(bucket:""{Bucket}"")");
        sb.AppendLine($"  |> range(start: {startFlux}, stop: {stopFlux})");
        sb.AppendLine(@"  |> keep(columns: [""_measurement"", ""name""])");
        sb.AppendLine(@"  |> distinct(column: ""_measurement"")");
        sb.AppendLine(@"  |> group()");

        var fluxTypes = sb.ToString();
        var typeTables = await queryApi.QueryAsync(fluxTypes, Org);

        var types = new HashSet<string>();
        foreach (var table in typeTables)
        {
            foreach (var record in table.Records)
            {
                if (record.Values.TryGetValue("_measurement", out var val) && val != null)
                    types.Add(val.ToString() ?? throw new ArgumentNullException("Measurement value is not defined"));
            }
        }

        var sbRooms = new StringBuilder();
        sbRooms.AppendLine($@"from(bucket:""{Bucket}"")");
        sbRooms.AppendLine($"  |> range(start: {startFlux}, stop: {stopFlux})");
        sbRooms.AppendLine(@"  |> keep(columns: [""name""])");
        sbRooms.AppendLine(@"  |> distinct(column: ""name"")");

        var fluxRooms = sbRooms.ToString();
        var roomTables = await queryApi.QueryAsync(fluxRooms, Org);

        var rooms = new HashSet<string>();
        foreach (var table in roomTables)
        {
            foreach (var record in table.Records)
            {
                if (record.Values.TryGetValue("name", out var val) && val != null)
                    rooms.Add(val.ToString() ?? throw new ArgumentNullException("Room name is null"));
            }
        }

        return new GetMetricsMetadataResponse
        {
            Types = types.ToList(),
            Rooms = rooms.ToList()
        };
    }

    public async Task<List<ISensorEvent>> GetMetricsAsync(
        GetMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var stringBuilder = new StringBuilder();

        var startFlux = request.From.HasValue ? request.From.Value.ToString("o") : "0";
        var stopFlux = request.To.HasValue ? request.To.Value.ToString("o") : "now()";
        stringBuilder.AppendLine($@"from(bucket:""{Bucket}"")");
        stringBuilder.AppendLine($"  |> range(start: {startFlux}, stop: {stopFlux})");

        if (!string.IsNullOrWhiteSpace(request.Room))
        {
            stringBuilder.AppendLine($@"  |> filter(fn: (r) => r[""name""] == ""{request.Room}"")");
        }

        stringBuilder.AppendLine(@"  |> pivot(rowKey: [""_time""], columnKey: [""_field""], valueColumn: ""_value"")");

        stringBuilder.AppendLine(@"  |> group()");

        stringBuilder.AppendLine(@"  |> sort(columns: [""_time""], desc: true)");

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            stringBuilder.AppendLine(@"  |> filter(fn: (r) => r[""_measurement""] == ""energy"")");
        }

        if (request.Take is not null && request.Skip is not null)
        {
            stringBuilder.AppendLine($@"  |> limit(n: {request.Take}, offset: {request.Skip})");
        }

        var flux = stringBuilder.ToString();

        var tables = await queryApi.QueryAsync(flux, Org);

        var result = new List<ISensorEvent>();

        foreach (var table in tables)
        {
            var fluxRecords = table.Records;

            foreach (var fluxRecord in fluxRecords)
            {
                if (!fluxRecord.Values.TryGetValue("_measurement", out var measurementType))
                {
                    throw new ArgumentNullException("Unable to parse measurment type");
                }

                if (!fluxRecord.Values.TryGetValue("name", out var roomObject))
                {
                    throw new ArgumentNullException("Unable to parse room from flux record");
                }

                var recordRoom = roomObject?.ToString() ?? throw new ArgumentNullException("Unable to parse room name to string");

                var time = fluxRecord.GetTime().GetValueOrDefault().ToDateTimeUtc();

                switch (measurementType)
                {
                    case "energy":
                        result.Add(new EnergyEvent
                        {
                            Room = recordRoom,
                            Time = time,
                            Energy = Convert.ToDouble(fluxRecord.Values["energy"])
                        });
                        break;

                    case "motion":
                        result.Add(new MotionEvent
                        {
                            Room = recordRoom,
                            Time = time,
                            MotionDetected = fluxRecord.Values["motionDetected"].ToString() == "1"
                        });
                        break;

                    case "air_quality":
                        result.Add(new AirQualityEvent
                        {
                            Room = recordRoom,
                            Time = time,
                            Co2 = Convert.ToInt32(fluxRecord.Values["co2"]),
                            Pm25 = Convert.ToInt32(fluxRecord.Values["pm25"]),
                            Humidity = Convert.ToInt32(fluxRecord.Values["humidity"]),
                        });
                        break;
                }
            }
        }

        return result;
    }

    public async Task<List<GetLatestMotionDataResponseModel>> GetLatestMotionDataAsync(
        GetLatestMotionDataQuery query,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var start = query.Timestamp ?? DateTime.UtcNow;

        var flux = $@"
from(bucket: ""{Bucket}"")
  |> range(start: 0, stop: {(query.UseLatestValue == false ? start.ToString("yyyy-MM-ddTHH:mm:ssZ") : "now()")})
  |> filter(fn: (r) => r[""_measurement""] == ""motion"")
  |> filter(fn: (r) => r[""_field""] == ""motionDetected"")
  |> group(columns: [""name""])
  |> sort(columns: [""_time""], desc: true)
  |> first()";

        try
        {
            var tables = await queryApi.QueryAsync(flux, Org, cancellationToken);

            var result = new List<GetLatestMotionDataResponseModel>();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var roomName = record.GetValueByKey("name")?.ToString();
                    if (string.IsNullOrWhiteSpace(roomName))
                        continue;

                    var motionValue = Convert.ToBoolean(record.GetValue());

                    result.Add(new GetLatestMotionDataResponseModel
                    {
                        Room = roomName,
                        MotionDetected = motionValue,
                    });
                }
            }

            return result;
        }
        catch (BadRequestException exception) when (exception.Message.Contains("cannot query an empty range"))
        {
            return [];
        }
    }

    public async Task<List<GetLatestMetricsResponseModel>> GetLatestMetricsAsync(
        GetLatestMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var sb = new StringBuilder();

        sb.AppendLine($@"from(bucket: ""{Bucket}"")");

        if (query.UseLatestValue || query.Timestamp == null)
        {
            sb.AppendLine(@"  |> range(start: 0)");
        }
        else
        {
            var stop = query.Timestamp.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            sb.AppendLine($@"  |> range(start: 0, stop: {stop})");
        }

        sb.AppendLine(@"  |> group(columns: [""name"", ""_measurement"", ""_field""])");

        sb.AppendLine(@"  |> last()");

        sb.AppendLine(@"  |> pivot(rowKey: [""name""], columnKey: [""_field""], valueColumn: ""_value"")");

        var flux = sb.ToString();

        var tables = await queryApi.QueryAsync(flux, Org, cancellationToken);

        var result = new List<GetLatestMetricsResponseModel>();

        var systemFields = new HashSet<string>
        {
            "result", "table", "_start", "_stop", "_measurement", "_time"
        };

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                var room = record.GetValueByKey("name")?.ToString();
                if (string.IsNullOrWhiteSpace(room))
                    continue;

                var telemetry = record.Values
                    .Where(kv => kv.Key != "name" && !systemFields.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? throw new ArgumentNullException("Telemetry value is not defined"));

                var existing = result.FirstOrDefault(r => r.Room == room);
                if (existing != null)
                {
                    foreach (var kv in telemetry)
                        existing.Telemetry[kv.Key] = kv.Value ?? throw new ArgumentNullException("Telemetry value is not defined");
                }
                else
                {
                    result.Add(new GetLatestMetricsResponseModel
                    {
                        Room = room,
                        Telemetry = telemetry
                    });
                }
            }
        }

        return result;
    }

    public async Task<List<GetAirQualitySummaryResponseModel>> GetAirQualitySummaryAsync(
        GetAirQualitySummaryQuery query,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var sb = new StringBuilder();

        sb.AppendLine($@"from(bucket: ""{Bucket}"")");

        if (query.UseLatestValue || query.Timestamp == null)
        {
            sb.AppendLine(@"  |> range(start: 0)");
        }
        else
        {
            var stop = query.Timestamp.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            sb.AppendLine($@"  |> range(start: 0, stop: {stop})");
        }

        sb.AppendLine(@"  |> filter(fn: (r) => r._measurement == ""air_quality"")");

        sb.AppendLine(@"  |> filter(fn: (r) => 
        r._field == ""co2"" or 
        r._field == ""pm25"" or 
        r._field == ""humidity"")");

        sb.AppendLine(@"  |> group(columns: [""name"", ""_field""])");
        sb.AppendLine(@"  |> last()");
        sb.AppendLine(@"  |> pivot(rowKey: [""name""], columnKey: [""_field""], valueColumn: ""_value"")");
        sb.AppendLine(@"  |> keep(columns: [""name"", ""co2"", ""pm25"", ""humidity""])");

        var flux = sb.ToString();

        var tables = await queryApi.QueryAsync(flux, Org, cancellationToken);

        var result = new List<GetAirQualitySummaryResponseModel>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                var room = record.GetValueByKey("name")?.ToString();
                if (string.IsNullOrWhiteSpace(room))
                    continue;

                result.Add(new GetAirQualitySummaryResponseModel
                {
                    Room = room,
                    Co2 = Convert.ToInt32(record.GetValueByKey("co2") ?? 0),
                    Pm25 = Convert.ToInt32(record.GetValueByKey("pm25") ?? 0),
                    Humidity = Convert.ToInt32(record.GetValueByKey("humidity") ?? 0)
                });
            }
        }

        return result;
    }

    public async Task<List<GetPowerConsumptionSummaryResponseModel>> GetPowerConsumptionSummaryAsync(
        GetPowerConsumptionSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var queryApi = Influx.GetQueryApi();

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($@"from(bucket:""{Bucket}"")");

        var start = query.From.HasValue
            ? query.From.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            : "0";

        var stop = query.To.HasValue
            ? query.To.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            : "now()";

        stringBuilder.AppendLine($@"  |> range(start: {start}, stop: {stop})");

        stringBuilder.AppendLine(@"  |> filter(fn: (r) => r[""_measurement""] == ""energy"")");

        stringBuilder.AppendLine(@"  |> group(columns: [""name""])");
        stringBuilder.AppendLine(@"  |> sum(column: ""_value"")");

        stringBuilder.AppendLine(@"  |> sort(columns: [""_value""], desc: true)");

        var flux = stringBuilder.ToString();

        var tables = await queryApi.QueryAsync(flux, Org, cancellationToken);

        var result = new List<GetPowerConsumptionSummaryResponseModel>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                var roomName = record.Values.TryGetValue("name", out var r) ? r?.ToString() : throw new ArgumentNullException(
                    "roomName",
                    "Name value for room is not defined");

                var energyValue = record.Values.TryGetValue("_value", out var e) ? Convert.ToDouble(e) : 0;

                if (!string.IsNullOrWhiteSpace(roomName))
                {
                    result.Add(new GetPowerConsumptionSummaryResponseModel
                    {
                        Room = roomName,
                        Energy = energyValue
                    });
                }
            }
        }

        return result;
    }
}
