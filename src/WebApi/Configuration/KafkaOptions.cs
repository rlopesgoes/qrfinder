namespace WebApi.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ProgressNotificationsTopic { get; set; } = "video.progress.notifications";
}