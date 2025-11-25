using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace Assessment.Gateway.Tests.Integration;

[Collection("SequentialTests")]
public class GraphQLEndpointTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphQLEndpointTests()
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
    public async Task GetMetrics_ShouldReturnEnergyEvents_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.5 }
        }, timestamp);

        // Небольшая задержка для обработки данных
        await Task.Delay(1000);

        var query = @"
        {
            metrics(room: ""room1"", type: ""energy"") {
                ... on EnergyEvent {
                    type
                    room
                    time
                    energy
                }
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("energy", content);
        Assert.Contains("room1", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnMotionEvents_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("motion", "room1", new Dictionary<string, object>
        {
            { "motionDetected", 1 }
        }, timestamp);

        await Task.Delay(1000);

        var query = @"
        {
            metrics(room: ""room1"") {
                ... on MotionEvent {
                    type
                    room
                    time
                    motionDetected
                }
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("motion", content);
        Assert.Contains("room1", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnAirQualityEvents_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("air_quality", "room1", new Dictionary<string, object>
        {
            { "co2", 400 },
            { "pm25", 10 },
            { "humidity", 50 }
        }, timestamp);

        await Task.Delay(1000);

        var query = @"
        {
            metrics(room: ""room1"") {
                ... on AirQualityEvent {
                    type
                    room
                    time
                    co2
                    pm25
                    humidity
                }
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("air_quality", content);
        Assert.Contains("room1", content);
        Assert.Contains("400", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetPowerConsumptionSummary_ShouldReturnSummary_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 }
        }, timestamp);

        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 50.0 }
        }, timestamp.AddMinutes(1));

        await Task.Delay(1000);

        var query = @"
        {
            powerConsumptionSummary {
                room
                energy
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("room1", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetLatestMotionData_ShouldReturnLatestData_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("motion", "room1", new Dictionary<string, object>
        {
            { "motionDetected", 1 }
        }, timestamp);

        await Task.Delay(1000);

        var query = @"
        {
            latestMotionData(useLatestValue: true) {
                room
                motionDetected
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("room1", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetLatestTelemetry_ShouldReturnLatestMetrics_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("energy", "room1", new Dictionary<string, object>
        {
            { "energy", 100.0 },
            { "temperature", 22.5 }
        }, timestamp);

        await Task.Delay(1000);

        var query = @"
        {
            latestTelemetry(useLatestValue: true) {
                room
                telemetry {
                    key
                    value
              }
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("room1", content);

        await ClearBucket();
    }

    [Fact]
    public async Task GetAirQualitySummary_ShouldReturnSummary_WhenDataExists()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        await WriteTestData("air_quality", "room1", new Dictionary<string, object>
        {
            { "co2", 400 },
            { "pm25", 10 },
            { "humidity", 50 }
        }, timestamp);

        await Task.Delay(1000);

        var query = @"
        {
            airQualitySummary {
                room
                co2
                pm25
                humidity
            }
        }";

        var request = new
        {
            query = query
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("room1", content);
        Assert.Contains("400", content);

        await ClearBucket();
    }
}

