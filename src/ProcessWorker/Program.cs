using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using Worker;
using Worker.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configure observability (logging + tracing) - centralized
builder.Services.AddObservability();

// Configure Kafka options from appsettings
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? kafkaOptions.GroupId;

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

// Configure Kafka consumer for this worker
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
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

// Configure Kafka producer for results publishing
builder.Services.AddSingleton<IProducer<string, byte[]>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrap
    };
    return new ProducerBuilder<string, byte[]>(config).Build();
});

// Configure Kafka producer for progress notifications
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrap
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// Register progress topic for Infrastructure services
var progressTopic = Environment.GetEnvironmentVariable("KAFKA_PROGRESS_NOTIFICATIONS_TOPIC") ?? kafkaOptions.ProgressNotificationsTopic;
builder.Services.AddSingleton(progressTopic);

builder.Services.AddHostedService<VideoControlConsumer>();

var host = builder.Build();
host.Run();
