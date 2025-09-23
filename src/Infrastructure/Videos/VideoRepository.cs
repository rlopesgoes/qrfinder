using Domain.Videos;
using Domain.Videos.Ports;
using Application.Videos.Ports.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Videos;

public class VideoRepository : IVideoRepository
{
    private readonly IMongoCollection<VideoDocument> _collection;

    public VideoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<VideoDocument>("videos");
    }

    public async Task<Video?> GetByIdAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        var document = await _collection
            .Find(x => x.Id == videoId.ToString())
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToAggregate();
    }

    public async Task SaveAsync(Video video, CancellationToken cancellationToken = default)
    {
        var document = VideoDocument.FromAggregate(video);
        
        await _collection.ReplaceOneAsync(
            x => x.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        video.ClearDomainEvents(); // Clear after persistence
    }

    public async Task<IReadOnlyList<Video>> GetByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default)
    {
        var documents = await _collection
            .Find(x => x.Status == status.ToString())
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToAggregate()).ToList();
    }

    // MongoDB document mapping
    private class VideoDocument
    {
        public string Id { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Upload tracking
        public int LastSequenceNumber { get; set; }
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        
        // Processing
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessingCompletedAt { get; set; }
        public int TotalFramesProcessed { get; set; }
        public List<QrCodeDocument> QrCodes { get; set; } = new();

        public static VideoDocument FromAggregate(Video video)
        {
            return new VideoDocument
            {
                Id = video.Id.ToString(),
                Status = video.Status.ToString(),
                CreatedAt = video.CreatedAt,
                UpdatedAt = video.UpdatedAt,
                ErrorMessage = video.ErrorMessage,
                LastSequenceNumber = video.LastSequenceNumber,
                ReceivedBytes = video.ReceivedBytes,
                TotalBytes = video.TotalBytes,
                ProcessingStartedAt = video.Metrics?.StartedAt,
                ProcessingCompletedAt = video.Metrics?.CompletedAt,
                TotalFramesProcessed = video.Metrics?.TotalFramesProcessed ?? 0,
                QrCodes = video.QrCodes.Select(qr => new QrCodeDocument
                {
                    Content = qr.Content,
                    TimestampSeconds = qr.DetectedAt.Seconds,
                    FormattedTimestamp = qr.FormattedTimestamp
                }).ToList()
            };
        }

        public Video ToAggregate()
        {
            var videoId = VideoId.From(Id);
            var status = Enum.Parse<VideoStatus>(Status);
            
            ProcessingMetrics? metrics = null;
            if (ProcessingStartedAt.HasValue)
            {
                metrics = new ProcessingMetrics(
                    ProcessingStartedAt.Value,
                    ProcessingCompletedAt,
                    TotalFramesProcessed,
                    ProcessingCompletedAt?.Subtract(ProcessingStartedAt.Value) ?? TimeSpan.Zero);
            }
            
            var qrCodes = QrCodes.Select(qr => QrCode.Create(qr.Content, qr.TimestampSeconds)).ToList();
            
            return Video.Reconstruct(
                videoId, status, CreatedAt, UpdatedAt,
                LastSequenceNumber, ReceivedBytes, TotalBytes, ErrorMessage,
                metrics, qrCodes);
        }
    }

    private class QrCodeDocument
    {
        public string Content { get; set; } = null!;
        public double TimestampSeconds { get; set; }
        public string FormattedTimestamp { get; set; } = null!;
    }
}