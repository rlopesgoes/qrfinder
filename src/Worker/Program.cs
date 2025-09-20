using DotNetEnv;
using Worker;

DotNetEnv.Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Sample>();

var host = builder.Build();
host.Run();
