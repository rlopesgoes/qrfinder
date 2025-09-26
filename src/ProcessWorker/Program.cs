using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configure telemetry (tracing + logging) 
builder.Services.AddTelemetry(builder.Configuration, "ProcessWorker");

// Configure Serilog manually for HostApplicationBuilder
var seqUrl = builder.Configuration.GetConnectionString("Seq") ?? "http://localhost:5342";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ServiceName", "ProcessWorker")
    .Enrich.WithOpenTelemetryTraceId()
    .Enrich.WithOpenTelemetrySpanId()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] ProcessWorker | TraceId: {TraceId} | SpanId: {SpanId} | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Logging.ClearProviders().AddSerilog();

var bootstrap = builder.Configuration.GetConnectionString("Kafka") ?? "localhost:9092";
var groupId = builder.Configuration.GetValue<string>("Kafka:GroupId") ?? "videos-worker-simple";
Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "qrfinder", "videos"));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Consumer para control
builder.Services.AddKeyedSingleton<IConsumer<string, byte[]>>("ControlConsumer", (sp, key) =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId + "-control",
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

builder.Services.AddHostedService<VideoControlConsumer>();

builder.Services.AddSingleton<IProducer<string, byte[]>>(_ =>
{
    var conf = new ProducerConfig
    {
        BootstrapServers = bootstrap,
        EnableIdempotence = true,
        Acks = Acks.All,
        LingerMs = 0,
        BatchSize = 524_288
    };
    return new ProducerBuilder<string, byte[]>(conf).Build();
});

var host = builder.Build();
host.Run();
