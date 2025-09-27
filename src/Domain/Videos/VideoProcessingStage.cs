namespace Domain.Videos;

public enum VideoProcessingStage
{
    Created,
    Sent,
    Uploading,
    Uploaded,
    Processing,
    Processed,
    Failed
}