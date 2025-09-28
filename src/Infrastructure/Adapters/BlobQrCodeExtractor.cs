using Application.Ports;
using Domain.Common;
using Domain.Models;

namespace Infrastructure.Adapters;

public class BlobQrCodeExtractor : IQrCodeExtractor
{
    private readonly QrCodeDetector _detector;
    private readonly IVideosReadOnlyRepository _videosReadOnlyRepository;
    private readonly IVideosWriteOnlyRepository _videosWriteOnlyRepository;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "processing");

    public BlobQrCodeExtractor(
        IVideosReadOnlyRepository videosReadOnlyRepository, 
        IVideosWriteOnlyRepository videosWriteOnlyRepository)
    {
        _detector = new QrCodeDetector();
        _videosReadOnlyRepository = videosReadOnlyRepository;
        _videosWriteOnlyRepository = videosWriteOnlyRepository;
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<Result<QrCodes>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        // Use the original string representation with hyphens (as uploaded)
        var videoIdString = videoId.Value.ToString(); // Keep hyphens to match upload format
        
        // 1. Download video from Azurite and create temporary file for processing
        var videoPath = await DownloadVideoForProcessingAsync(videoIdString, cancellationToken);
        
        try
        {
            // 2. Extract QR codes using technical detector
            var detections = await _detector.DetectQrCodesAsync(videoPath, cancellationToken);
            
            return new QrCodes(detections);
        }
        finally
        {
            // 4. Cleanup temporary processing file
            CleanupVideoFile(videoPath);
            
            // 5. Delete video from Azurite after processing (as requested)
            await _videosWriteOnlyRepository.DeleteAsync(videoIdString, cancellationToken);
        }
    }

    private async Task<string> DownloadVideoForProcessingAsync(string videoId, CancellationToken cancellationToken)
    {
        var videoPath = Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");
        
        // Stream download directly to disk without loading into memory
        await _videosReadOnlyRepository.GetIntoLocalFolderAsync(videoId, videoPath, cancellationToken);
        
        return videoPath;
    }

    private static void CleanupVideoFile(string videoPath)
    {
        try
        {
            if (File.Exists(videoPath))
                File.Delete(videoPath);
        }
        catch 
        {
            // Best effort cleanup
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        return result.Trim();
    }
}