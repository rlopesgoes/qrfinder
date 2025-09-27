namespace Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "video-notifications";
    public string ProgressNotificationsTopic { get; set; } = "video.progress.notifications";
    public string GroupId { get; set; } = "default-group";
}