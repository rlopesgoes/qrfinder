using Application.Videos.Ports;
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

    public async Task<IReadOnlyList<QrCode>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        // Use the original string representation, not the GUID with hyphens
        var videoIdString = videoId.Value.ToString("N"); // "N" format removes hyphens
        
        // 1. Download video from Azurite and create temporary file for processing
        var videoPath = await DownloadVideoForProcessingAsync(videoIdString, cancellationToken);
        
        try
        {
            // 2. Extract QR codes using technical detector
            var detections = await _detector.DetectQrCodesAsync(videoPath, cancellationToken);
            
            // 3. Convert to domain objects
            var qrCodes = detections.Select(d => QrCode.Create(d.Text, d.TimestampSeconds)).ToList();
            
            return qrCodes;
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
        
        // Delete existing file if present
        if (File.Exists(videoPath))
            File.Delete(videoPath);

        // Get video stream from Azurite (assembled from chunks)
        using var videoStream = await _blobStorage.GetVideoStreamAsync(videoId, cancellationToken);
        using var fileStream = new FileStream(videoPath, FileMode.Create, FileAccess.Write);
        
        await videoStream.CopyToAsync(fileStream, cancellationToken);
        
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