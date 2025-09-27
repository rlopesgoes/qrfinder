using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using NotificationService.Channels;
using NotificationService.Services;
using NotificationsWorker.Configuration;
using MediatR;

var builder = Host.CreateApplicationBuilder(args);

// Configure Kafka options from appsettings
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddInfrastructure();
builder.Services.AddObservability();

// Add MediatR and register the specific handler we need
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<Application.UseCases.SendNotifications.SendNotificationsHandler>();
});

// Configure Kafka Consumer
var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? kafkaOptions.Topic;
var groupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? kafkaOptions.GroupId;

builder.Services.AddSingleton<IConsumer<Ignore, string>>(_ =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Latest,
        EnableAutoCommit = false
    };
    return new ConsumerBuilder<Ignore, string>(config).Build();
});

builder.Services.AddSingleton(topic);

// Add Kafka consumer service
builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

app.Run();