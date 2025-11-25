using Assessment.Gateway.Features.GetMetricsMetadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace Assessment.Gateway.Tests.Integration;

[Collection("SequentialTests")]
public class RestEndpointTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RestEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Influx:Url", InfluxConfiguration.InfluxUrl },
                    { "Influx:Org", InfluxConfiguration.InfluxOrg },
                    { "Influx:Bucket", InfluxConfiguration.InfluxBucket }
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetMetricsMetadata_ShouldReturnTypesAndRooms_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 }
        }, timestamp);

        await WriteTestData("motion", "room2", new Dictionary<string, object>
        {
            { "motionDetected", 1 }
        }, timestamp);

        await Task.Delay(1000);

        // Act
        var response = await _client.GetAsync("/metrics/metadata");
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<GetMetricsMetadataResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Contains("energy", result.Types);
        Assert.Contains("motion", result.Types);
        Assert.Contains("room1", result.Rooms);
        Assert.Contains("room2", result.Rooms);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetricsMetadata_WithFromAndTo_ShouldFilterByDateRange()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-2);
        var to = DateTime.UtcNow.AddDays(-1);
        var timestamp = DateTime.UtcNow;

        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 }
        }, timestamp);

        await Task.Delay(1000);

        // Act
        var response = await _client.GetAsync($"/metrics/metadata?from={from:O}&to={to:O}");
        var result = await response.Content.ReadFromJsonAsync<GetMetricsMetadataResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetricsMetadata_WithRoom_ShouldFilterByRoom()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 }
        }, timestamp);

        await WriteTestData("energy", "room2", new Dictionary<string, object>
        {
            { "energy", 200.0 }
        }, timestamp);

        await Task.Delay(1000);

        // Act
        var response = await _client.GetAsync("/metrics/metadata?room=room1");
        var result = await response.Content.ReadFromJsonAsync<GetMetricsMetadataResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetricsMetadata_WithMetricType_ShouldFilterByType()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 }
        }, timestamp);

        await WriteTestData("motion", "room1", new Dictionary<string, object>
        {
            { "motionDetected", 1 }
        }, timestamp);

        await Task.Delay(1000);

        // Act
        var response = await _client.GetAsync("/metrics/metadata?metricType=energy");
        var result = await response.Content.ReadFromJsonAsync<GetMetricsMetadataResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetricsMetadata_ShouldReturnEmptyLists_WhenNoData()
    {
        // Act
        var response = await _client.GetAsync("/metrics/metadata");
        var result = await response.Content.ReadFromJsonAsync<GetMetricsMetadataResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Empty(result.Types);
        Assert.Empty(result.Rooms);
    }
}

