using Confluent.Kafka;

namespace WebApi.Messaging;

public static class MessagingConfiguration
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? kafkaOptions.BootstrapServers;
        var progressTopic = Environment.GetEnvironmentVariable("KAFKA_PROGRESS_NOTIFICATIONS_TOPIC") ?? kafkaOptions.ProgressNotificationsTopic;

        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        services.AddSingleton(progressTopic);
        
        return services;
    }
}