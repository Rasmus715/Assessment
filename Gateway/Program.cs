using Assessment.Gateway;
using Assessment.Gateway.Features.GetAirQualitySummary;
using Assessment.Gateway.Features.GetAirQualitySummary.Models;
using Assessment.Gateway.Features.GetLatestMetrics;
using Assessment.Gateway.Features.GetLatestMetrics.Models;
using Assessment.Gateway.Features.GetLatestMotionData;
using Assessment.Gateway.Features.GetMetrics;
using Assessment.Gateway.Features.GetMetricsMetadata;
using Assessment.Gateway.Features.GetPowerConsumptionSummary;
using Assessment.Gateway.Features.GetPowerConsumptionSummary.Models;
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL;
using Assessment.Gateway.Features.GetSensorEvents.Models.GraphQL.Abstraction;
using Assessment.Gateway.Services;
using Assessment.Gateway.Services.Abstraction;
using InfluxDB.Client;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddTransient<IInfluxDBClient, InfluxDBClient>(sp =>
{
    var url = builder.Configuration["Influx:Url"] ?? "http://localhost:8086";
    return new InfluxDBClient(url, "admin", "admin123");
});

builder.Services.AddTransient<IInfluxDbService, InfluxDbService>();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSeq(
        builder.Configuration["SEQ_SERVER_URL"] ?? throw new ArgumentNullException("Seq Server Url is not defined"),
        "SeqTokenForApps12345", enrichers: [
            (evt) => evt.AddOrUpdateProperty("ApplicationType", "Gateway")]
        );
});

builder.Services
    .AddGraphQLServer()

    // --- Интерфейсы и их реализации ---
    .AddInterfaceType<ISensorEvent>(descriptor =>
    {
        descriptor.Name("SensorEvent");
        descriptor.Field(f => f.Type).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Room).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Time).Type<NonNullType<DateTimeType>>();
    })
    .AddType<EnergyEvent>()
    .AddType<MotionEvent>()
    .AddType<AirQualityEvent>()

    // --- DTO для сводки ---
    .AddType<GetPowerConsumptionSummaryResponseModel>()
    .AddType<GetAirQualitySummaryResponseModel>()
    .AddType<GetLatestMotionDataQueryHandler>()
    .AddType<GetLatestMetricsResponseModel>()

    .AddQueryType(q => q.Name("Query"))
    // --- Query типы ---
    .AddType<GetMetricsGraphQLQuery>()
    .AddType<GetPowerConsumptionSummaryGraphQLQuery>()
    .AddType<GetLatestMotionDataGraphQLQuery>()
    .AddType<GetAirQualitySummaryGraphQLQuery>()
    .AddType<GetLatestMetricsGraphQLQuery>()

    // --- (опционально) фильтры, сортировки, пагинация и т.д. ---
    .AddFiltering()
    .AddSorting();

builder.Services.AddMediator();

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowCredentials()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .SetIsOriginAllowed(hostName => true);
}));

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

app.UseCors("MyPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapGraphQL("/graphql");

app.MapGet("metrics/metadata", async (
    [AsParameters] GetMetricsMetadataRequest request,
    [FromServices] IMediator mediator) =>
{
    var query = new GetMetricsMetadataQuery
    {
        From = request.From,
        To = request.To,
        Room = request.Room,
        MetricType = request.MetricType
    };

    var result = await mediator.Send(query);

    return result;
});

app.MapHub<AssessmentHub>("/assessment");

app.Run();

public partial class Program { }
