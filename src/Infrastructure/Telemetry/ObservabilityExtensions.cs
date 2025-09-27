using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;

namespace Infrastructure.Telemetry;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        var observabilityConfig = GetObservabilityConfig();
        
        ConfigureLogging(observabilityConfig);
        services.AddLogging(builder => builder.ClearProviders().AddSerilog());
        
        ConfigureTracing(services, observabilityConfig);
        
        return services;
    }
    
    private static ObservabilityConfig GetObservabilityConfig()
    {
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "QrFinder";
                         
        return new ObservabilityConfig
        {
            ServiceName = serviceName,
            SeqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5342",
            JaegerOtlpUrl = Environment.GetEnvironmentVariable("JAEGER_OTLP_URL") ?? "http://localhost:4318/v1/traces",
            EnableConsoleExporter = Environment.GetEnvironmentVariable("ENABLE_CONSOLE_EXPORTER") == "true",
            EnableResourceLogging = Environment.GetEnvironmentVariable("ENABLE_RESOURCE_LOGGING") == "true"
        };
    }
    
    private static void ConfigureLogging(ObservabilityConfig config)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("ServiceName", config.ServiceName)
            .Enrich.WithOpenTelemetryTraceId()
            .Enrich.WithOpenTelemetrySpanId()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} | TraceId: {TraceId} | SpanId: {SpanId} | {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(config.SeqUrl)
            .CreateLogger();
    }
    
    private static void ConfigureTracing(IServiceCollection services, ObservabilityConfig config)
    {
        services.AddSingleton<ActivitySource>(sp => new ActivitySource(config.ServiceName));
        
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(config.ServiceName, "1.0.0")
                        .AddTelemetrySdk())
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddSource(config.ServiceName);
                
                if (config.EnableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }
                
                builder.AddOtlpExporter(options => options.Endpoint = new Uri(config.JaegerOtlpUrl));
            });
    }
    
    private record ObservabilityConfig
    {
        public required string ServiceName { get; init; }
        public required string SeqUrl { get; init; }
        public required string JaegerOtlpUrl { get; init; }
        public required bool EnableConsoleExporter { get; init; }
        public required bool EnableResourceLogging { get; init; }
    }

    public static IHostBuilder UseLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog();
    }
}