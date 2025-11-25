using Testcontainers.InfluxDb;

namespace Assessment.Gateway.Tests.Integration;

public class IntegrationTestFixture : IAsyncLifetime
{


    public async Task InitializeAsync()
    {
        InfluxConfiguration.InfluxContainer = new InfluxDbBuilder()
            .WithImage("influxdb:2.7")
            .WithUsername("admin")
            .WithPassword("admin123")
            .WithOrganization(InfluxConfiguration.InfluxOrg)
            .WithBucket(InfluxConfiguration.InfluxBucket)
            .Build();

        await InfluxConfiguration.InfluxContainer.StartAsync();

        await InitializeInfluxDb();
    }

    public async Task DisposeAsync()
    {
        if (InfluxConfiguration.InfluxContainer != null)
        {
            await InfluxConfiguration.InfluxContainer.DisposeAsync();
        }
    }

    private async Task InitializeInfluxDb()
    {
        // InfluxDB 2.x автоматически создает бакет при первом запуске
        // Просто ждем, пока контейнер полностью запустится
        await Task.Delay(10000);

        InfluxConfiguration.InfluxUrl = $"http://{InfluxConfiguration.InfluxContainer?.Hostname}:{InfluxConfiguration.InfluxContainer?.GetMappedPublicPort(8086)}";
    }
}

