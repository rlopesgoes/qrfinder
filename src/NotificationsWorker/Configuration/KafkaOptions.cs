namespace NotificationService.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "video.progress.notifications";
    public string GroupId { get; set; } = "notification-service";
    public string ProgressNotificationsTopic { get; set; } = "progress.notifications";
}