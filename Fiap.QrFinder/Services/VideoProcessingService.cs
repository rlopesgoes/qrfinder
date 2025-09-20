using System.Drawing;
using FFMpegCore;
using Fiap.QrFinder.Models;
using ZXing;
using ZXing.Windows.Compatibility;

namespace Fiap.QrFinder.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string _uploadFolder;

    public VideoProcessingService(ILogger<VideoProcessingService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _uploadFolder = Path.Combine(environment.ContentRootPath, "uploads");

        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }

        // Set the PATH environment variable to include /usr/bin
        try
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!path.Contains("/usr/bin"))
            {
                Environment.SetEnvironmentVariable("PATH", path + ":/usr/bin");
                _logger.LogInformation("Added /usr/bin to PATH environment variable");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting PATH environment variable");
        }
    }

    public async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        var videoId = Guid.NewGuid().ToString("N");
        var fileName = $"{videoId}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }

    public async Task<VideoProcessingResult> ProcessVideoAsync(string filePath, int frameInterval = 1)
    {
        _logger.LogInformation("Processing video: {FilePath}", filePath);

        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);
            var frameRate = mediaInfo.PrimaryVideoStream!.FrameRate;
            var duration = mediaInfo.Duration;

            // Calculate total frames based on duration and frame rate
            var estimatedFrames = (int)(duration.TotalSeconds * frameRate);

            var result = new VideoProcessingResult
            {
                VideoId = Path.GetFileNameWithoutExtension(filePath).Split('_')[0],
                FileName = Path.GetFileName(filePath),
                TotalFrames = estimatedFrames,
                DurationInSeconds = duration.TotalSeconds
            };

            // Create temporary directory for extracted frames
            var tempDir = Path.Combine(_uploadFolder, "temp_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract frames using FFMpeg
                await FFMpegArguments
                    .FromFileInput(filePath)
                    .OutputToFile(Path.Combine(tempDir, "frame-%04d.png"), true, options => options
                        .WithCustomArgument($"-vf \"fps=1/{frameInterval}\""))
                    .ProcessAsynchronously();

                // Process extracted frames to find QR codes
                var frameFiles = Directory.GetFiles(tempDir, "frame-*.png").OrderBy(f => f).ToArray();

                // Create a QR code reader configured for QR codes
                var reader = new BarcodeReader
                {
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                        TryInverted = true
                    },
                    AutoRotate = true
                };

                for (int i = 0; i < frameFiles.Length; i++)
                {
                    var frameFile = frameFiles[i];
                    var frameNumber = (i + 1) * frameInterval;

                    try
                    {
                        using (var bitmap = new Bitmap(frameFile))
                        {
                            // Use the BarcodeReader to find multiple barcodes
                            var results = reader.DecodeMultiple(bitmap);

                            if (results != null)
                            {
                                foreach (var qrCode in results)
                                {
                                    result.QrCodes.Add(new QrCodeResult
                                    {
                                        Content = qrCode.Text,
                                        FrameNumber = frameNumber,
                                        TimeInSeconds = frameNumber / frameRate,
                                        Location = new QrCodeLocation
                                        {
                                            X = qrCode.ResultPoints.Min(p => (int)p.X),
                                            Y = qrCode.ResultPoints.Min(p => (int)p.Y),
                                            Width = (int)(qrCode.ResultPoints.Max(p => p.X) - qrCode.ResultPoints.Min(p => p.X)),
                                            Height = (int)(qrCode.ResultPoints.Max(p => p.Y) - qrCode.ResultPoints.Min(p => p.Y))
                                        }
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing frame {FrameNumber}", frameNumber);
                    }
                }
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessVideoAsync: {Message}", ex.Message);
            throw;
        }
    }
}