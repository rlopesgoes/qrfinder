using Application;
using Confluent.Kafka;
using Infrastructure;
using Infrastructure.Telemetry;
using Microsoft.OpenApi.Models;
using WebApi.Configuration;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configure Kafka options from appsettings
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

var kafkaOptions = builder.Configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
var progressTopic = Environment.GetEnvironmentVariable("KAFKA_PROGRESS_NOTIFICATIONS_TOPIC") ?? kafkaOptions.ProgressNotificationsTopic;

// Configure Kafka Producer for Infrastructure
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// Register progress topic for Infrastructure services
builder.Services.AddSingleton(progressTopic);

// Configure observability (logging + tracing) - centralized
builder.Host.UseLogging();
builder.Services.AddObservability();
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(name: "v1", new OpenApiInfo { Title = "QrFinder API", Version = "v1" });
});

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapVideosEndpoints();

app.Run();