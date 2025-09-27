using Application.Videos.Ports;
using Infrastructure.Configuration;
using Infrastructure.Implementations;
using Infrastructure.Queues;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? options.ConnectionString;
            options.Database = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? options.Database;
        });
        
        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>();
            return new MongoClient(options.Value.ConnectionString);
        });
        
        services.AddSingleton(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>();
            return mongoClient.GetDatabase(options.Value.Database);
        });

        services.Configure<BlobStorageOptions>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? options.ConnectionString;
        });
        
        services.AddScoped<IAnalysisStatusRepository, AnalysisStatusRepository>();
        services.AddScoped<IVideoProcessingRepository, VideoProcessingRepository>();
        services.AddScoped<IResultsPublisher, Videos.KafkaResultsPublisher>();
        services.AddScoped<IVideoChunkStorage, Videos.FileVideoChunkStorage>();
        services.AddScoped<IAnalyzeProgressNotifier, Notifiers.KafkaAnalyzeProgressNotifier>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<Domain.Videos.Ports.IQrCodeExtractor, Videos.BlobQrCodeExtractor>();
        services.AddScoped<IUploadLinkGenerator, UploadLinkGenerator>();
        services.AddScoped<IVideoAnalysisQueue, KafkaVideoAnalysisQueue>();
        services.AddScoped<INotificationChannel, SignalRServerChannel>();

        return services;
    }
}