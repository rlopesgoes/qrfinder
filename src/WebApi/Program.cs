using DotNetEnv;
using Application;
using Application.Queries;
using MediatR;
using Microsoft.OpenApi.Models;
using WebApi;
using WebApi.Endpoints;

Env.TraversePath().Load();
        
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(name: "v1", new OpenApiInfo { Title = "QrFinder API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapVideosEndpoints();

app.Run();