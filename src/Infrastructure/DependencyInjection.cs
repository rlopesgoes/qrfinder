using Application.Videos.Common;
using Application.Videos.Data;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Infrastructure.Implementations;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IVideoUploader, KafkaVideoUploader>();
        services.AddScoped<IVideoStatusRepository, VideoStatusRepository>();
        services.AddScoped<IVideoProcessingRepository, VideoProcessingRepository>();
        
        var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")!;
        services.AddSingleton<IProducer<string, byte[]>>(_ =>
            new ProducerBuilder<string, byte[]>(new ProducerConfig 
            {
                BootstrapServers = bootstrap, 
                Acks = Acks.All, 
                EnableIdempotence = true, 
                LingerMs = 0,
                BatchSize = 524288,        // 512 KB (igual ao ChunkSize)
                
            }).Build());
        
        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrap
        };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification
                {
                    Name = "videos.control",
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });
            
            adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification
                {
                    Name = "videos.raw-chunks",
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });
            
            adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification
                {
                    Name = "videos.results",
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });

            Console.WriteLine("Tópico criado com sucesso.");
        }
        catch (CreateTopicsException e)
        {
            if (e.Results[0].Error.Code != ErrorCode.TopicAlreadyExists)
            {
                Console.WriteLine($"Erro ao criar tópico: {e.Results[0].Error.Reason}");
            }
            else
            {
                Console.WriteLine("Tópico já existe.");
            }
        }
        
        var mongoUser = Environment.GetEnvironmentVariable("MONGODB_USER")!;
        var mongoPassword = Environment.GetEnvironmentVariable("MONGODB_PASSWORD")!;
        var mongoHost = Environment.GetEnvironmentVariable("MONGODB_HOST")!;
        var mongoPort = Environment.GetEnvironmentVariable("MONGODB_PORT")!;
        var mongoDb = Environment.GetEnvironmentVariable("MONGODB_DB")!;
        var connectionString = $"mongodb://{mongoUser}:{mongoPassword}@{mongoHost}:{mongoPort}/{mongoDb}?authSource=admin";
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDb));
        
        return services;
    }
}