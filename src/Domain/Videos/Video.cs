using Domain.Common;

namespace Domain.Videos;

public class Video : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly List<QrCode> _qrCodes = new();

    private Video() { } // EF Constructor

    private Video(VideoId id, VideoStatus status)
    {
        Id = id;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Factory for reconstruction from persistence
    public static Video Reconstruct(
        VideoId id, VideoStatus status, DateTime createdAt, DateTime updatedAt,
        int lastSeq, long receivedBytes, long totalBytes, string? errorMessage,
        ProcessingMetrics? metrics, IReadOnlyList<QrCode> qrCodes)
    {
        var video = new Video
        {
            Id = id,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastSequenceNumber = lastSeq,
            ReceivedBytes = receivedBytes,
            TotalBytes = totalBytes,
            ErrorMessage = errorMessage,
            Metrics = metrics
        };
        
        video._qrCodes.AddRange(qrCodes);
        return video;
    }

    public VideoId Id { get; private set; } = null!;
    public VideoStatus Status { get; private set; }
    public ProcessingMetrics? Metrics { get; private set; }
    public IReadOnlyList<QrCode> QrCodes => _qrCodes.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Upload tracking properties (preserving original logic)
    public int LastSequenceNumber { get; private set; } = -1;
    public long ReceivedBytes { get; private set; }
    public long TotalBytes { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Video Create(VideoId id) => new(id, VideoStatus.Created);

    public void StartUpload(long totalBytes)
    {
        if (Status != VideoStatus.Created)
            throw new InvalidOperationException($"Cannot start upload for video in {Status} status");

        if (totalBytes <= 0)
            throw new ArgumentException("Total bytes must be greater than zero");

        Status = VideoStatus.Uploading;
        TotalBytes = totalBytes;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VideoChunkReceived(Id, -1, 0));
    }

    public void ReceiveChunk(int sequenceNumber, long chunkSize)
    {
        if (Status != VideoStatus.Uploading)
            throw new InvalidOperationException($"Cannot receive chunk for video in {Status} status");

        if (sequenceNumber < 0)
            throw new ArgumentException("Sequence number cannot be negative");

        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than zero");

        LastSequenceNumber = Math.Max(LastSequenceNumber, sequenceNumber);
        ReceivedBytes += chunkSize;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VideoChunkReceived(Id, sequenceNumber, ReceivedBytes));
    }

    public void CompleteUpload()
    {
        if (Status != VideoStatus.Uploading)
            throw new InvalidOperationException($"Cannot complete upload for video in {Status} status");

        Status = VideoStatus.Uploaded;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VideoUploadCompleted(Id));
    }

    public void StartProcessing()
    {
        if (Status != VideoStatus.Uploaded)
            throw new InvalidOperationException($"Cannot start processing for video in {Status} status");

        Status = VideoStatus.Processing;
        Metrics = ProcessingMetrics.Started();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VideoProcessingStarted(Id));
    }

    public ProcessingResult CompleteProcessing(IReadOnlyList<QrCodeDetection> detections)
    {
        if (Status != VideoStatus.Processing)
            throw new InvalidOperationException($"Cannot complete processing for video in {Status} status");

        if (Metrics == null)
            throw new InvalidOperationException("Processing metrics not initialized");

        // Convert detections to QR codes with validation
        var qrCodes = new List<QrCode>();
        foreach (var detection in detections)
        {
            var qrCode = QrCode.Create(detection.Text, detection.TimestampSeconds);
            qrCodes.Add(qrCode);
        }

        // Update aggregate state
        _qrCodes.Clear();
        _qrCodes.AddRange(qrCodes);
        
        Status = VideoStatus.Processed;
        Metrics = Metrics.Complete(detections.Count);
        UpdatedAt = DateTime.UtcNow;

        var result = new ProcessingResult(Id, QrCodes, Metrics);
        AddDomainEvent(new VideoProcessingCompleted(Id, QrCodes, Metrics));

        return result;
    }

    public void FailProcessing(string errorMessage)
    {
        if (Status != VideoStatus.Processing)
            throw new InvalidOperationException($"Cannot fail processing for video in {Status} status");

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty");

        Status = VideoStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VideoProcessingFailed(Id, errorMessage));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

// Preserving original detection structure exactly
public record QrCodeDetection(string Text, double TimestampSeconds);

public record ProcessingResult(
    VideoId VideoId,
    IReadOnlyList<QrCode> QrCodes,
    ProcessingMetrics Metrics);

public enum VideoStatus
{
    Created,
    Uploading,
    Uploaded,
    Processing,
    Processed,
    Failed
}