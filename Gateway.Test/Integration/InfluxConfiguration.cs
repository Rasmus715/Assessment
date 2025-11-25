using Testcontainers.InfluxDb;

namespace Assessment.Gateway.Tests.Integration;

public static class InfluxConfiguration
{
    public static string InfluxUrl { get; set; }

    public static string InfluxOrg => "test_org";

    public static string InfluxBucket => "test_bucket";

    public static InfluxDbContainer? InfluxContainer { get; set; }
}
