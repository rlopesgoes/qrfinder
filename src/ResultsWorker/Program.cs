using System.Runtime.CompilerServices;
using Application;
using Confluent.Kafka;
using DotNetEnv;
using Infrastructure;
using ResultsWorker;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = "localhost:9092";
var groupId = "results-worker";

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddSingleton<IConsumer<string, byte[]>>(_ =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Latest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

builder.Services.AddHostedService<ResultsProcessor>();

var host = builder.Build();
host.Run();