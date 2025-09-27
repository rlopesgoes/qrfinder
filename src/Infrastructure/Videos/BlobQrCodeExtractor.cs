using Application.Videos.Ports;
using Domain.Common;
using Domain.Videos;
using Domain.Videos.Ports;

namespace Infrastructure.Videos;

public class BlobQrCodeExtractor : IQrCodeExtractor
{
    private readonly QrCodeDetector _detector;
    private readonly IBlobStorageService _blobStorage;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "processing");

    public BlobQrCodeExtractor(IBlobStorageService blobStorage)
    {
        _detector = new QrCodeDetector();
        _blobStorage = blobStorage;
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
            
            // 3. Convert to domain objects
            var qrCodes = detections.Select(d => QrCode.Create(d.Text, d.TimestampSeconds)).ToList();
            
            return new QrCodes(qrCodes);
        }
        finally
        {
            // 4. Cleanup temporary processing file
            CleanupVideoFile(videoPath);
            
            // 5. Delete video from Azurite after processing (as requested)
            await _blobStorage.DeleteVideoAsync(videoIdString, cancellationToken);
        }
    }

    private async Task<string> DownloadVideoForProcessingAsync(string videoId, CancellationToken cancellationToken)
    {
        var videoPath = Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");
        
        // Stream download directly to disk without loading into memory
        return await _blobStorage.DownloadVideoDirectlyAsync(videoId, videoPath, cancellationToken);
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