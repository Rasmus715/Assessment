using Assessment.Gateway.Features.GetAirQualitySummary;
using Assessment.Gateway.Features.GetLatestMetrics;
using Assessment.Gateway.Features.GetLatestMotionData;
using Assessment.Gateway.Features.GetMetrics;
using Assessment.Gateway.Features.GetMetricsMetadata;
using Assessment.Gateway.Features.GetPowerConsumptionSummary;
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;
using Assessment.Gateway.Services;
using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Assessment.Gateway.Tests.Unit;

public class InfluxDbServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly IInfluxDBClient _influxClient;
    private readonly IQueryApi _queryApi;
    private readonly InfluxDbService _service;

    public InfluxDbServiceTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _influxClient = Substitute.For<IInfluxDBClient>();
        _queryApi = Substitute.For<IQueryApi>();

        _configuration["Influx:Org"].Returns("test_org");
        _configuration["Influx:Bucket"].Returns("test_bucket");

        _influxClient.GetQueryApi().Returns(_queryApi);

        _service = new InfluxDbService(_configuration, _influxClient);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues_WhenConfigurationIsNull()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config["Influx:Org"].Returns((string?)null);
        config["Influx:Bucket"].Returns((string?)null);
        var client = Substitute.For<IInfluxDBClient>();

        // Act
        var service = new InfluxDbService(config, client);

        // Assert
        Assert.Equal("rasmus", service.Org);
        Assert.Equal("weakapp_data", service.Bucket);
        Assert.Equal(client, service.Influx);
    }

    [Fact]
    public void Constructor_ShouldUseConfigurationValues_WhenProvided()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config["Influx:Org"].Returns("custom_org");
        config["Influx:Bucket"].Returns("custom_bucket");
        var client = Substitute.For<IInfluxDBClient>();

        // Act
        var service = new InfluxDbService(config, client);

        // Assert
        Assert.Equal("custom_org", service.Org);
        Assert.Equal("custom_bucket", service.Bucket);
    }

    [Fact]
    public async Task GetMetricsMetadataAsync_ShouldReturnTypesAndRooms_WhenDataExists()
    {
        // Arrange
        var query = new GetMetricsMetadataQuery
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow
        };

        var typeTable = CreateFluxTable(new Dictionary<string, object?>
        {
            { "_measurement", "energy" }
        });

        var roomTable = CreateFluxTable(new Dictionary<string, object?>
        {
            { "name", "room1" }
        });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { typeTable }),
                     Task.FromResult<List<FluxTable>>(new List<FluxTable> { roomTable }));

        // Act
        var result = await _service.GetMetricsMetadataAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("energy", result.Types);
        Assert.Contains("room1", result.Rooms);
    }

    [Fact]
    public async Task GetMetricsMetadataAsync_ShouldUseDefaultDates_WhenNotProvided()
    {
        // Arrange
        var query = new GetMetricsMetadataQuery();

        var emptyTable = CreateFluxTable(new Dictionary<string, object?>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        var result = await _service.GetMetricsMetadataAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _queryApi.Received().QueryAsync(Arg.Is<string>(s => s.Contains("range(start: 0")), "test_org");
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnEnergyEvent_WhenEnergyDataExists()
    {
        // Arrange
        var query = new GetMetricsQuery
        {
            Room = "room1",
            Type = "energy"
        };

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "_measurement", "energy" },
            { "name", "room1" },
            { "energy", 100.5 },
            { "_time", new NodaTime.Instant() }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetMetricsAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var energyEvent = result[0] as EnergyEvent;
        Assert.NotNull(energyEvent);
        Assert.Equal("room1", energyEvent.Room);
        Assert.Equal(100.5, energyEvent.Energy);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMotionEvent_WhenMotionDataExists()
    {
        // Arrange
        var query = new GetMetricsQuery();

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "_measurement", "motion" },
            { "name", "room1" },
            { "motionDetected", "1" },
            { "_time", new NodaTime.Instant() }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetMetricsAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var motionEvent = result[0] as MotionEvent;
        Assert.NotNull(motionEvent);
        Assert.Equal("room1", motionEvent.Room);
        Assert.True(motionEvent.MotionDetected);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnAirQualityEvent_WhenAirQualityDataExists()
    {
        // Arrange
        var query = new GetMetricsQuery();

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "_measurement", "air_quality" },
            { "name", "room1" },
            { "co2", 400 },
            { "pm25", 10 },
            { "humidity", 50 },
            { "_time", new NodaTime.Instant() }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetMetricsAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var airQualityEvent = result[0] as AirQualityEvent;
        Assert.NotNull(airQualityEvent);
        Assert.Equal("room1", airQualityEvent.Room);
        Assert.Equal(400, airQualityEvent.Co2);
        Assert.Equal(10, airQualityEvent.Pm25);
        Assert.Equal(50, airQualityEvent.Humidity);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldThrowException_WhenMeasurementTypeIsMissing()
    {
        // Arrange
        var query = new GetMetricsQuery();

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "name", "room1" },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetMetricsAsync(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldFilterByRoom_WhenRoomIsProvided()
    {
        // Arrange
        var query = new GetMetricsQuery
        {
            Room = "room1"
        };

        var emptyTable = CreateFluxTableWithRecords(Array.Empty<FluxRecord>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        var result = await _service.GetMetricsAsync(query, CancellationToken.None);

        // Assert
        _queryApi.Received().QueryAsync(Arg.Is<string>(s => s.Contains("r[\"name\"] == \"room1\"")), "test_org");
    }

    [Fact]
    public async Task GetLatestMotionDataAsync_ShouldReturnMotionData_WhenDataExists()
    {
        // Arrange
        var query = new GetLatestMotionDataQuery
        {
            UseLatestValue = true
        };

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "name", "room1" },
            { "_value", true },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetLatestMotionDataAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("room1", result[0].Room);
        Assert.True(result[0].MotionDetected);
    }

    [Fact]
    public async Task GetLatestMotionDataAsync_ShouldReturnEmptyList_WhenBadRequestExceptionWithEmptyRange()
    {
        // Arrange
        var query = new GetLatestMotionDataQuery
        {
            UseLatestValue = true
        };

        var exception = new BadRequestException("cannot query an empty range");

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<FluxTable>>(exception));

        // Act
        var result = await _service.GetLatestMotionDataAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestMotionDataAsync_ShouldUseTimestamp_WhenUseLatestValueIsFalse()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var query = new GetLatestMotionDataQuery
        {
            Timestamp = timestamp,
            UseLatestValue = false
        };

        var emptyTable = CreateFluxTableWithRecords(Array.Empty<FluxRecord>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        await _service.GetLatestMotionDataAsync(query, CancellationToken.None);

        // Assert
        _queryApi.Received().QueryAsync(
            Arg.Is<string>(s => s.Contains(timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"))),
            "test_org",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLatestMetricsAsync_ShouldReturnMetrics_WhenDataExists()
    {
        // Arrange
        var query = new GetLatestMetricsQuery
        {
            UseLatestValue = true
        };

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "name", "room1" },
            { "temperature", "22.5" },
            { "humidity", "60" },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetLatestMetricsAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("room1", result[0].Room);
        Assert.Contains("temperature", result[0].Telemetry.Keys);
        Assert.Contains("humidity", result[0].Telemetry.Keys);
    }

    [Fact]
    public async Task GetLatestMetricsAsync_ShouldUseTimestamp_WhenProvided()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var query = new GetLatestMetricsQuery
        {
            Timestamp = timestamp,
            UseLatestValue = false
        };

        var emptyTable = CreateFluxTableWithRecords(Array.Empty<FluxRecord>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        await _service.GetLatestMetricsAsync(query, CancellationToken.None);

        // Assert
        _queryApi.Received().QueryAsync(
            Arg.Is<string>(s => s.Contains(timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))),
            "test_org",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAirQualitySummaryAsync_ShouldReturnAirQualityData_WhenDataExists()
    {
        // Arrange
        var query = new GetAirQualitySummaryQuery
        {
            UseLatestValue = true
        };

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "name", "room1" },
            { "co2", 400 },
            { "pm25", 10 },
            { "humidity", 50 },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetAirQualitySummaryAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("room1", result[0].Room);
        Assert.Equal(400, result[0].Co2);
        Assert.Equal(10, result[0].Pm25);
        Assert.Equal(50, result[0].Humidity);
    }

    [Fact]
    public async Task GetAirQualitySummaryAsync_ShouldUseTimestamp_WhenProvided()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var query = new GetAirQualitySummaryQuery
        {
            Timestamp = timestamp,
            UseLatestValue = false
        };

        var emptyTable = CreateFluxTableWithRecords(Array.Empty<FluxRecord>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        await _service.GetAirQualitySummaryAsync(query, CancellationToken.None);

        // Assert
        _queryApi.Received().QueryAsync(
            Arg.Is<string>(s => s.Contains(timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))),
            "test_org",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPowerConsumptionSummaryAsync_ShouldReturnPowerData_WhenDataExists()
    {
        // Arrange
        var query = new GetPowerConsumptionSummaryQuery
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow
        };

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "name", "room1" },
            { "_value", 1500.75 },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act
        var result = await _service.GetPowerConsumptionSummaryAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("room1", result[0].Room);
        Assert.Equal(1500.75, result[0].Energy);
    }

    [Fact]
    public async Task GetPowerConsumptionSummaryAsync_ShouldUseDefaultDates_WhenNotProvided()
    {
        // Arrange
        var query = new GetPowerConsumptionSummaryQuery();

        var emptyTable = CreateFluxTableWithRecords(Array.Empty<FluxRecord>());

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { emptyTable }));

        // Act
        await _service.GetPowerConsumptionSummaryAsync(query, CancellationToken.None);

        // Assert
        _queryApi.Received().QueryAsync(
            Arg.Is<string>(s => s.Contains("range(start: 0") || s.Contains("range(start: 0,")),
            "test_org");
    }

    [Fact]
    public async Task GetPowerConsumptionSummaryAsync_ShouldThrowException_WhenRoomNameIsMissing()
    {
        // Arrange
        var query = new GetPowerConsumptionSummaryQuery();

        var time = DateTimeOffset.UtcNow;
        var record = CreateFluxRecord(new Dictionary<string, object?>
        {
            { "_value", 1500.75 },
            { "_time", time }
        });

        var table = CreateFluxTableWithRecords(new[] { record });

        _queryApi.QueryAsync(Arg.Any<string>(), "test_org")
            .Returns(Task.FromResult<List<FluxTable>>(new List<FluxTable> { table }));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetPowerConsumptionSummaryAsync(query, CancellationToken.None));
    }

    // Helper methods to create FluxTable and FluxRecord
    private FluxTable CreateFluxTable(Dictionary<string, object?> values)
    {
        var table = new FluxTable();
        var record = CreateFluxRecord(values);
        table.Records.Add(record);
        return table;
    }

    private FluxTable CreateFluxTableWithRecords(FluxRecord[] records)
    {
        var table = new FluxTable();
        foreach (var record in records)
        {
            table.Records.Add(record);
        }
        return table;
    }

    private FluxRecord CreateFluxRecord(Dictionary<string, object?> values)
    {
        var record = new FluxRecord(0);
        foreach (var kvp in values)
        {
            record.Values[kvp.Key] = kvp.Value;
        }

        return record;
    }
}
