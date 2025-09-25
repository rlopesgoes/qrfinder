using Confluent.Kafka;
using Infrastructure;
using ResultsWorker;

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = builder.Configuration.GetConnectionString("Kafka") ?? "localhost:9092";
var groupId = builder.Configuration.GetValue<string>("Kafka:GroupId") ?? "videos-worker-results";

builder.Services.AddInfrastructure(builder.Configuration);

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