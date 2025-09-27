using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using ResultsWorker;

var builder = Host.CreateApplicationBuilder(args);

// Configure observability (logging + tracing) - centralized
builder.Services.AddObservability();
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? "videos-worker-results";

builder.Services.AddInfrastructure();

builder.Services.AddSingleton<IConsumer<string, byte[]>>(_ =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

builder.Services.AddHostedService<ResultsProcessor>();

var host = builder.Build();
host.Run();