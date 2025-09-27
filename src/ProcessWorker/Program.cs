using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configure observability (logging + tracing) - centralized
builder.Services.AddObservability();

var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? "videos-worker-simple";

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddKeyedSingleton<IConsumer<string, string>>("ControlConsumer", (sp, key) =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId + "-control",
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
    };
    return new ConsumerBuilder<string, string>(conf).Build();
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
