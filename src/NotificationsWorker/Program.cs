using Application.UseCases.SendNotifications;
using Confluent.Kafka;
using Infrastructure;
using NotificationService.Configuration;
using NotificationService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddInfrastructure();
builder.Services.AddObservability();

builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<SendNotificationsHandler>();
});

var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
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

builder.Services.AddHostedService<NotificationConsumer>();

var app = builder.Build();

app.Run();