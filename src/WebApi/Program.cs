using Application;
using Infrastructure;
using Microsoft.OpenApi.Models;
using WebApi.Endpoints;
using WebApi.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseLogging();

builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddObservability();
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(name: "v1", new OpenApiInfo { Title = "QrFinder API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

WebApplication app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapVideosEndpoints();

app.Run();

public partial class Program { }