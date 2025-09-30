using Application.UseCases.SaveAnalysisResults;
using Confluent.Kafka;
using Infrastructure;
using ResultsWorker.Configuration;
using ResultsWorker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddObservability();
builder.Services.AddInfrastructure();

builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<SaveAnalysisResultsHandler>();
});

var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? kafkaOptions.GroupId;

builder.Services.AddSingleton<IConsumer<string, byte[]>>(_ =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Latest
    };
    return new ConsumerBuilder<string, byte[]>(conf).Build();
});

builder.Services.AddHostedService<ResultsConsumer>();

var host = builder.Build();

host.Run();