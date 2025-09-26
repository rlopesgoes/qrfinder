using Application;
using Infrastructure;
using Infrastructure.Telemetry;
using Microsoft.OpenApi.Models;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Configure observability (logging + tracing) - centralized
builder.Host.UseLogging();
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
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