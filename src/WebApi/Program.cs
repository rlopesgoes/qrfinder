using DotNetEnv;
using Application;
using Application.Videos.Common;
using Infrastructure;
using Microsoft.OpenApi.Models;
using WebApi.Endpoints;
using WebApi.Hubs;
using WebApi.Notifiers;

Env.TraversePath().Load();
        
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddSignalR();
builder.Services.AddScoped<IProgressNotifier, SignalRProgressNotifier>();

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