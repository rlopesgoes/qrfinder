using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;

namespace Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        // Configure Serilog with OpenTelemetry enricher
        var seqUrl = configuration.GetConnectionString("Seq") ?? "http://localhost:5342";
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithOpenTelemetryTraceId()
            .Enrich.WithOpenTelemetrySpanId()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} | TraceId: {TraceId} | SpanId: {SpanId} | {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(seqUrl)
            .CreateLogger();

        // Add Serilog as the logging provider
        services.AddLogging(builder => builder.ClearProviders().AddSerilog());

        // Register Activity Source for the specific service
        var activitySourceName = $"QrFinder.{serviceName}";
        services.AddSingleton<ActivitySource>(sp => new ActivitySource(activitySourceName));

        // Configure OpenTelemetry Tracing
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService($"QrFinder.{serviceName}", "1.0.0")
                        .AddTelemetrySdk())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource(activitySourceName)
                    .AddConsoleExporter()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://localhost:4318/v1/traces");
                    });
            });

        return services;
    }

    public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog();
    }
}