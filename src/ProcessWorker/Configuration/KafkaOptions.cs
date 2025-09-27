namespace ProcessWorker.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "videos-worker-simple";
    public string ProgressNotificationsTopic { get; set; } = "progress.notifications";
}