using Domain.Videos;
using Domain.Videos.Ports;
using Application.Videos.Ports;

namespace Infrastructure.Videos;

public class QrCodeExtractor : IQrCodeExtractor
{
    private readonly IQrCodeDetector _detector;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");

    public QrCodeExtractor(IQrCodeDetector detector)
    {
        _detector = detector;
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<IReadOnlyList<QrCode>> ExtractFromVideoAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        // 1. Assemble video file from chunks
        var videoPath = AssembleVideoFile(videoId);
        
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
            // 4. Cleanup temporary files
            CleanupVideoFile(videoPath);
        }
    }

    private string AssembleVideoFile(VideoId videoId)
    {
        var partPath = Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId.ToString())}.bin.part");
        var finalPath = Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId.ToString())}.bin");

        if (!File.Exists(partPath))
            throw new FileNotFoundException($"Video chunks not found for {videoId}");

        if (File.Exists(finalPath))
            File.Delete(finalPath);

        File.Move(partPath, finalPath);
        return finalPath;
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