using System.Runtime.CompilerServices;
using Application;
using Confluent.Kafka;
using DotNetEnv;
using Infrastructure;
using Worker;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = "localhost:9092";
var topicChunks = "videos.raw-chunks";
var topicControl =  "videos.control";
var topicResults =  "videos.results";
var groupId     =  "videos-worker-simple";
var workDir     =  "/data/videos"; // pasta para .part/.bin
Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "qrfinder", "videos"));

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.AddHostedService<Sample>();

builder.Services.AddSingleton<IConsumer<string, byte[]>>(_ =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
        FetchMaxBytes = 2 * 1024 * 1024,
        MaxPartitionFetchBytes = 2 * 1024 * 1024,
        QueuedMaxMessagesKbytes = 1024
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

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

builder.Services.AddHostedService<Sample>();


var host = builder.Build();
host.Run();
