using Application;
using Confluent.Kafka;
using DotNetEnv;
using Infrastructure;
using Worker;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = "localhost:9092";
var groupId = "videos-worker-simple";
Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "qrfinder", "videos"));

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
// Consumer para chunks
builder.Services.AddKeyedSingleton<IConsumer<string, byte[]>>("ChunkConsumer", (sp, key) =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId + "-chunks",
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
        FetchMaxBytes = 2 * 1024 * 1024,
        MaxPartitionFetchBytes = 2 * 1024 * 1024,
        QueuedMaxMessagesKbytes = 1024
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

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

builder.Services.AddHostedService<VideoChunkConsumer>();
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
