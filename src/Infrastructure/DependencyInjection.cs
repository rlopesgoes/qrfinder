using Application.Videos.Ports;
using Confluent.Kafka;
using Infrastructure.Configuration;
using Infrastructure.Implementations;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Configure options with environment variable overrides
        services.Configure<KafkaOptions>(options =>
        {
            options.BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? options.BootstrapServers;
            options.Topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? options.Topic;
        });

        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? options.ConnectionString;
            options.Database = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? options.Database;
        });

        services.Configure<BlobStorageOptions>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? options.ConnectionString;
        });

        // Register services
        services.AddScoped<IVideoUploader, BlobVideoUploader>();
        services.AddScoped<IVideoStatusRepository, VideoStatusRepository>();
        services.AddScoped<IVideoProcessingRepository, VideoProcessingRepository>();
        services.AddScoped<IResultsPublisher, Videos.KafkaResultsPublisher>();
        services.AddScoped<IVideoChunkStorage, Videos.FileVideoChunkStorage>();
        services.AddScoped<IVideoProgressNotifier, Notifiers.KafkaVideoProgressNotifier>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<Domain.Videos.Ports.IQrCodeExtractor, Videos.BlobQrCodeExtractor>();
        
        // Register MongoDB
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
        
        // Register Kafka Producer  
        services.AddSingleton<IProducer<string, byte[]>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>();
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers
            };
            return new ProducerBuilder<string, byte[]>(config).Build();
        });
        
        return services;
    }
}