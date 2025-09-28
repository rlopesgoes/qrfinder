using Application.Ports;
using Infrastructure.Adapters;
using Infrastructure.Configuration;
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
        
        services.AddScoped<IStatusReadOnlyRepository, StatusReadOnlyRepository>();
        services.AddScoped<IStatusWriteOnlyRepository, StatusWriteOnlyRepository>();
        services.AddScoped<IAnalysisResultReadOnlyRepository, AnalysisResultReadOnlyRepository>();
        services.AddScoped<IAnalysisResultWriteOnlyRepository, AnalysisResultWriteOnlyRepository>();
        services.AddScoped<IResultsPublisher, KafkaResultsPublisher>();
        services.AddScoped<IProgressNotifier, KafkaProgressNotifier>();
        services.AddScoped<IVideosReadOnlyRepository, VideosReadOnlyRepository>();
        services.AddScoped<IVideosWriteOnlyRepository, VideosWriteOnlyRepository>();
        services.AddScoped<IQrCodeScanner, QrCodeScannerWithFFmpeg>();
        services.AddScoped<IUploadLinkGenerator, UploadLinkGenerator>();
        services.AddScoped<IVideoAnalysisQueue, KafkaVideoAnalysisQueue>();
        services.AddScoped<INotificationChannel, SignalRServerChannel>();

        return services;
    }
}