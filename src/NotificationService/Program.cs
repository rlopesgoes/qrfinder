using Infrastructure.Telemetry;
using NotificationService.Channels;
using NotificationService.Hubs;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure observability (logging + tracing) - centralized
builder.Host.UseLogging();
builder.Services.AddObservability(builder.Configuration);

// Configure URLs from appsettings.json or default
var urls = builder.Configuration.GetValue<string>("Urls") ?? "http://localhost:5010";
builder.WebHost.UseUrls(urls);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add notification channels
builder.Services.AddScoped<INotificationChannel, SignalRServerChannel>();

// Add dispatcher
builder.Services.AddScoped<NotificationDispatcher>();

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