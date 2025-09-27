using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using NotificationService.Channels;
using NotificationService.Hubs;
using NotificationService.Services;
using NotificationsWorker.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Kafka options from appsettings
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

// Configure observability (logging + tracing) - centralized
builder.Host.UseLogging();
builder.Services.AddObservability();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Configure URLs - force port 5010 for NotificationsWorker
builder.WebHost.UseUrls("http://localhost:5010");

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add notification channels
builder.Services.AddScoped<INotificationChannel, SignalRServerChannel>();

// Add dispatcher
//builder.Services.AddScoped<NotificationDispatcher>();

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

// Configure Kafka producers for Infrastructure services
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers
    };
    return new ProducerBuilder<string, string>(config).Build();
});

builder.Services.AddSingleton<IProducer<string, byte[]>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers
    };
    return new ProducerBuilder<string, byte[]>(config).Build();
});

// Register progress topic for Infrastructure services
var progressTopic = Environment.GetEnvironmentVariable("KAFKA_PROGRESS_NOTIFICATIONS_TOPIC") ?? kafkaOptions.ProgressNotificationsTopic;
builder.Services.AddSingleton(progressTopic);

// Add Kafka consumer service
builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

app.MapGet("/health", () => Results.Ok("Notification Service is running"));

app.Run();