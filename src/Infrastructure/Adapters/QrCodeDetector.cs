using System.Collections.Concurrent;
using System.Globalization;
using Domain.Models;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace Infrastructure.Adapters;

internal class QrCodeDetector
{
    private const int DefaultFrameRate = 5;
    private const double FrameInterval = 0.2;

    public async Task<IReadOnlyCollection<QrCode>> DetectQrCodesAsync(string videoPath, CancellationToken cancellationToken)
    {
        var qrCodes = await DetectQrCodesInternalAsync(videoPath, cancellationToken);
        return qrCodes.Select(qr => new QrCode(qr.content, new (qr.timestamp))).ToList();
    }
    
    private static async Task<IReadOnlyCollection<(string content, double timestamp)>> DetectQrCodesInternalAsync(string videoPath, CancellationToken cancellationToken)
    {
        var framesDirectory = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");
        Directory.CreateDirectory(framesDirectory);

        var pattern = Path.Combine(framesDirectory, "frame-%06d.png");
        await RunFfmpegAsync("ffmpeg", $"-y -i \"{videoPath}\" -vf \"fps={DefaultFrameRate}\" \"{pattern}\"", cancellationToken);

        var videoTimestamps = await ExtractVideoTimestampsAsync(videoPath, cancellationToken);
        var files = Directory.GetFiles(framesDirectory, "frame-*.png").OrderBy(x => x).ToArray();
        var frameTimestamps = MapFramesToTimestamps(files.Length, videoTimestamps);

        var frameResults = new ConcurrentBag<(string text, double timestamp)>();

        await Parallel.ForEachAsync(
            files.Select((file, index) => new { file, index }),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken },
            (frameInfo, _) =>
            {
                try
                {
                    var qrResult = ProcessFrame(frameInfo.file);
                    if (qrResult?.Text != null)
                    {
                        var timestamp = frameInfo.index < frameTimestamps.Count
                            ? frameTimestamps.ToArray()[frameInfo.index]
                            : frameInfo.index * FrameInterval;
                        frameResults.Add((qrResult.Text, timestamp));
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) { }
                
                return ValueTask.CompletedTask;
            });

        List<(string content, double timestamp)> uniqueResults = frameResults
            .GroupBy(r => r.text)
            .Select(g => g.OrderBy(r => r.timestamp).First())
            .OrderBy(r => r.timestamp)
            .Select(r => (content: r.text, timestamp: r.timestamp))
            .ToList();

        try { Directory.Delete(framesDirectory, true); } catch { }
        return uniqueResults;
    }

    private static async Task RunFfmpegAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName, 
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true, 
            UseShellExecute = false
        })!;
        
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0) 
        {
            var errorMessage = $"{fileName} failed with exit code {process.ExitCode}.\n" +
                              $"Command: {fileName} {arguments}\n" +
                              $"STDERR: {stderr}\n" +
                              $"STDOUT: {stdout}";
            throw new Exception(errorMessage);
        }
    }

    private static async Task<IReadOnlyCollection<double>> ExtractVideoTimestampsAsync(string videoPath, CancellationToken cancellationToken)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=p=0 \"{videoPath}\"",
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        })!;
        
        var outStr = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var errorMessage = $"ffprobe failed with exit code {process.ExitCode}.\n" +
                              $"Command: ffprobe -v error -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=p=0 \"{videoPath}\"\n" +
                              $"STDERR: {stderr}\n" +
                              $"STDOUT: {outStr}";
            throw new Exception(errorMessage);
        }

        var list = new List<double>();
        foreach (var line in outStr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(line.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                list.Add(d);
        
        return list;
    }

    private static IReadOnlyCollection<double> MapFramesToTimestamps(int framesEmitted, IReadOnlyCollection<double> allVideoTimestamps)
    {
        if (framesEmitted == 0 || allVideoTimestamps.Count == 0) 
            return [];
        
        var result = new List<double>();
        
        if (allVideoTimestamps.Count < framesEmitted)
        {
            var duration = allVideoTimestamps.Last() - allVideoTimestamps.First();
            var interval = duration / (framesEmitted - 1);
            for (var i = 0; i < framesEmitted; i++)
                result.Add(allVideoTimestamps.First() + (i * interval));
            return result;
        }

        var videoDuration = allVideoTimestamps.Last() - allVideoTimestamps.First();
        var frameInterval = videoDuration / (framesEmitted - 1);
        for (var i = 0; i < framesEmitted; i++)
        {
            var targetTime = allVideoTimestamps.First() + (i * frameInterval);
            var closestTimestamp = allVideoTimestamps.OrderBy(timestamp => Math.Abs(timestamp - targetTime)).First();
            result.Add(closestTimestamp);
        }
        
        return result;
    }

    private static Result? ProcessFrame(string filePath)
    {
        using var bitmap = SKBitmap.Decode(filePath);
        if (bitmap is null) return null;

        var reader = CreateQrCodeReader();
        
        var result = reader.Decode(bitmap);
        if (result is not null)
            return result;
        
        result = TryScaledDecoding(reader, bitmap);
        if (result is not null)
            return result;
        
        result = TryInvertedDecoding(reader, bitmap);
        if (result is not null) 
            return result;
        
        result = TryHighContrastDecoding(reader, bitmap);
        if (result is not null)
            return result;
        
        result = TryBinarizedDecoding(reader, bitmap);
        
        return result;
    }

    private static BarcodeReader CreateQrCodeReader() =>
        new()
        {
            Options = { TryHarder = true, TryInverted = true, PureBarcode = false, PossibleFormats = new[] { BarcodeFormat.QR_CODE } },
            AutoRotate = true
        };

    private static Result? TryScaledDecoding(BarcodeReader reader, SKBitmap bitmap)
    {
        var scale = bitmap.Width < 800 ? 3 : 2;
        var scaledInfo = new SKImageInfo(bitmap.Width * scale, bitmap.Height * scale);
        using var scaledBitmap = new SKBitmap(scaledInfo);
        bitmap.ScalePixels(scaledBitmap, new SKSamplingOptions(SKFilterMode.Linear));
        return reader.Decode(scaledBitmap);
    }

    private static Result? TryInvertedDecoding(BarcodeReader reader, SKBitmap bitmap)
    {
        using var invertedBitmap = InvertColors(bitmap);
        return invertedBitmap != null ? reader.Decode(invertedBitmap) : null;
    }

    private static Result? TryHighContrastDecoding(BarcodeReader reader, SKBitmap bitmap)
    {
        using var contrastBitmap = HighContrast(bitmap);
        return contrastBitmap != null ? reader.Decode(contrastBitmap) : null;
    }

    private static Result? TryBinarizedDecoding(BarcodeReader reader, SKBitmap bitmap)
    {
        using var binaryBitmap = Binarize(bitmap);
        return binaryBitmap != null ? reader.Decode(binaryBitmap) : null;
    }

    private static SKBitmap? InvertColors(SKBitmap original)
    {
        try
        {
            var info = new SKImageInfo(original.Width, original.Height);
            var inverted = new SKBitmap(info);
            
            using var canvas = new SKCanvas(inverted);
            using var paint = new SKPaint();
            
            var invertMatrix = new float[]
            {
                -1, 0, 0, 0, 255,
                0, -1, 0, 0, 255,
                0, 0, -1, 0, 255,
                0, 0, 0, 1, 0
            };
            
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(invertMatrix);
            canvas.DrawBitmap(original, 0, 0, paint);
            
            return inverted;
        }
        catch
        {
            return null;
        }
    }

    private static SKBitmap? HighContrast(SKBitmap original)
    {
        try
        {
            var info = new SKImageInfo(original.Width, original.Height);
            var contrast = new SKBitmap(info);
            
            using var canvas = new SKCanvas(contrast);
            using var paint = new SKPaint();
            
            var contrastMatrix = new float[]
            {
                3, 0, 0, 0, -128,
                0, 3, 0, 0, -128,
                0, 0, 3, 0, -128,
                0, 0, 0, 1, 0
            };
            
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(contrastMatrix);
            canvas.DrawBitmap(original, 0, 0, paint);
            
            return contrast;
        }
        catch
        {
            return null;
        }
    }

    private static SKBitmap? Binarize(SKBitmap original)
    {
        try
        {
            var info = new SKImageInfo(original.Width, original.Height);
            var binary = new SKBitmap(info);
            
            using var canvas = new SKCanvas(binary);
            using var paint = new SKPaint();
            
            var binaryMatrix = new float[]
            {
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0, 0, 0, 1, 0
            };
            
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(binaryMatrix);
            canvas.DrawBitmap(original, 0, 0, paint);
            
            return binary;
        }
        catch
        {
            return null;
        }
    }
}