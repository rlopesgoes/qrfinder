using Application.UseCases.ScanQrCode;
using Confluent.Kafka;
using Infrastructure;
using Worker;
using Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddInfrastructure();
builder.Services.AddObservability();

builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<ScanQrCodeHandler>();
});

var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? kafkaOptions.GroupId;

builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var conf = new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Latest
    };
    return new ConsumerBuilder<string, string>(conf).Build();
});

builder.Services.AddSingleton<IProducer<string, byte[]>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrap
    };
    return new ProducerBuilder<string, byte[]>(config).Build();
});

builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrap
    };
    return new ProducerBuilder<string, string>(config).Build();
});

var consumerType = builder.Configuration.GetValue<string>("AnalysisWorker:ConsumerType", "Standard");
if (consumerType == "Parallel")
{
    builder.Services.AddHostedService<ParallelAnalysisConsumer>();
}
else
{
    builder.Services.AddHostedService<AnalysisConsumer>();
}

var host = builder.Build();

host.Run();
