using Application;
using Infrastructure;
using Infrastructure.Telemetry;
using Microsoft.OpenApi.Models;
using WebApi.Endpoints;
using WebApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure telemetry (tracing + logging)
builder.Host.ConfigureLogging();
builder.Services.AddTelemetry(builder.Configuration, "WebApi");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(name: "v1", new OpenApiInfo { Title = "QrFinder API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHub<UploadProgressHub>("/hubs/upload");
app.MapVideosEndpoints();

app.Run();