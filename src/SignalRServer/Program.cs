using Infrastructure.Telemetry;
using SignalRServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapHub<NotificationHub>("/notificationHub");

app.MapGet("/health", () => Results.Ok("SignalR Server is running"));

app.Urls.Add("http://localhost:5010");

app.Run();
