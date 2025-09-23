using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using MongoDB.Driver;

namespace Infrastructure.Implementations;

public class VideoProcessingRepository : IVideoProcessingRepository
{
    private readonly IMongoCollection<VideoProcessingDocument> _collection;

    public VideoProcessingRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<VideoProcessingDocument>("video_processing");
    }

    public async Task<VideoProcessingResult?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var document = await _collection
            .Find(x => x.VideoId == videoId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToResult();
    }

    public async Task SaveAsync(VideoProcessingResult videoProcessing, CancellationToken cancellationToken = default)
    {
        var document = VideoProcessingDocument.FromResult(videoProcessing);
        
        await _collection.ReplaceOneAsync(
            x => x.VideoId == document.VideoId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private class VideoProcessingDocument
    {
        public string Id { get; set; } = null!;
        public string VideoId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<QrCodeDocument> QrCodes { get; set; } = new();

        public static VideoProcessingDocument FromResult(VideoProcessingResult result)
        {
            return new VideoProcessingDocument
            {
                Id = result.VideoId,
                VideoId = result.VideoId,
                Status = result.Status,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt,
                QrCodes = result.QrCodes.Select(qr => new QrCodeDocument
                {
                    Text = qr.Text,
                    TimestampSeconds = qr.TimestampSeconds,
                    FormattedTimestamp = qr.FormattedTimestamp,
                    DetectedAt = qr.DetectedAt
                }).ToList()
            };
        }

        public VideoProcessingResult ToResult()
        {
            return new VideoProcessingResult(
                VideoId,
                Status,
                StartedAt,
                CompletedAt,
                QrCodes.Count,
                QrCodes.Select(qr => new QrCodeResultDto(
                    qr.Text,
                    qr.TimestampSeconds,
                    qr.FormattedTimestamp,
                    qr.DetectedAt)).ToList());
        }
    }

    private class QrCodeDocument
    {
        public string Text { get; set; } = null!;
        public double TimestampSeconds { get; set; }
        public string FormattedTimestamp { get; set; } = null!;
        public DateTime DetectedAt { get; set; }
    }
}