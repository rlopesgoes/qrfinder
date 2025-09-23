using Confluent.Kafka;
using DotNetEnv;
using Infrastructure;
using ResultsWorker;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = "localhost:9092";
var groupId = "videos-worker-results";

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