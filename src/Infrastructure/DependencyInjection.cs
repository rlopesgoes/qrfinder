using Application.Videos.Ports;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Infrastructure.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Application layer services
        services.AddScoped<IVideoUploader, KafkaVideoUploader>();
        services.AddScoped<IVideoStatusRepository, VideoStatusRepository>();
        services.AddScoped<IVideoProcessingRepository, VideoProcessingRepository>();
        services.AddScoped<IResultsPublisher, Infrastructure.Videos.KafkaResultsPublisher>();
        services.AddScoped<IVideoChunkStorage, Infrastructure.Videos.FileVideoChunkStorage>();
        services.AddScoped<IVideoProgressNotifier, Infrastructure.Notifiers.KafkaVideoProgressNotifier>();
        
        // Domain services (Clean Architecture)
        services.AddScoped<Domain.Videos.Ports.IQrCodeExtractor, Infrastructure.Videos.QrCodeExtractor>();
        
        // Use configuration or fallback to default values
        var bootstrap = configuration?.GetConnectionString("Kafka") ?? "localhost:9092";
        
        services.AddSingleton<IProducer<string, byte[]>>(_ =>
            new ProducerBuilder<string, byte[]>(new ProducerConfig 
            {
                BootstrapServers = bootstrap, 
                Acks = Acks.All, 
                EnableIdempotence = true, 
                LingerMs = 0,
                BatchSize = 524288,        // 512 KB (igual ao ChunkSize)
                
            }).Build());
        
        // MongoDB configuration from appsettings
        var mongoConnectionString = configuration?.GetConnectionString("MongoDB") ?? 
            "mongodb://admin:password123@localhost:27017/qrfinder";
        
        var mongoDatabase = configuration?.GetValue<string>("MongoDB:Database") ?? "qrfinder";
            
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
        
        return services;
    }
}