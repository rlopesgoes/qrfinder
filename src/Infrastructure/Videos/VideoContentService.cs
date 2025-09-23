using Domain.Videos;
using Domain.Videos.Ports;
using Application.Videos.Ports;
using System.Globalization;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace Infrastructure.Videos;

// Unifies all video technical operations: storage + QR detection
public class VideoContentService : IVideoContentService
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
    private const int ChunkBufferSize = 64 * 1024;
    private const int DefaultFrameRate = 5;
    private const double FrameInterval = 0.2;

    // Chunk storage operations (preserving exact original logic)
    public async Task StoreChunkAsync(VideoId videoId, byte[] chunkData, CancellationToken cancellationToken = default)
    {
        var partPath = GetPartVideoPath(videoId.ToString());
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);

        await using var fileStream = new FileStream(
            path: partPath,
            mode: FileMode.Append,
            access: FileAccess.Write,
            share: FileShare.Read,
            bufferSize: ChunkBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await fileStream.WriteAsync(chunkData.AsMemory(), cancellationToken);
    }

    public Task<string> FinalizeVideoAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        var partPath = GetPartVideoPath(videoId.ToString());
        var finalPath = GetCompleteVideoPath(videoId.ToString());

        if (!File.Exists(partPath))
            return Task.FromResult(finalPath);

        if (File.Exists(finalPath))
            File.Delete(finalPath);

        File.Move(partPath, finalPath);
        return Task.FromResult(finalPath);
    }

    public Task CleanupVideoAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(videoPath))
            File.Delete(videoPath);
        return Task.CompletedTask;
    }

    // QR Code detection with ALL original fallbacks preserved
    public async Task<IReadOnlyList<Domain.Videos.QrCodeDetection>> DetectQrCodesAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        var qrCodes = await DetectQrCodesInternalAsync(videoPath, cancellationToken);
        return qrCodes.Select(qr => new Domain.Videos.QrCodeDetection(qr.content, qr.timestamp)).ToList();
    }

    private async Task<IReadOnlyCollection<(string content, double timestamp)>> DetectQrCodesInternalAsync(string videoPath, CancellationToken cancellationToken)
    {
        var framesDirectory = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");
        Directory.CreateDirectory(framesDirectory);

        var pattern = Path.Combine(framesDirectory, "frame-%06d.png");
        await RunFfmpeg("ffmpeg", $"-y -i \"{videoPath}\" -vf \"fps={DefaultFrameRate}\" \"{pattern}\"", cancellationToken);

        var videoTimestamps = await ExtractVideoTimestamps(videoPath, cancellationToken);
        var files = Directory.GetFiles(framesDirectory, "frame-*.png").OrderBy(x => x).ToArray();
        var frameTimestamps = MapFramesToTimestamps(files.Length, videoTimestamps);

        var frameResults = new System.Collections.Concurrent.ConcurrentBag<(string text, double timestamp)>();

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
                            ? frameTimestamps[frameInfo.index]
                            : frameInfo.index * FrameInterval;
                        frameResults.Add((qrResult.Text, timestamp));
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) { }
                return ValueTask.CompletedTask;
            });

        var uniqueResults = frameResults
            .GroupBy(r => r.text)
            .Select(g => g.OrderBy(r => r.timestamp).First())
            .OrderBy(r => r.timestamp)
            .Select(r => (content: r.text, timestamp: r.timestamp))
            .ToList();

        try { Directory.Delete(framesDirectory, true); } catch { }
        return uniqueResults;
    }

    private static async Task RunFfmpeg(string exe, string args, CancellationToken cancellationToken)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe, Arguments = args,
            RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false
        })!;
        _ = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new Exception($"{exe} failed");
    }

    private static async Task<List<double>> ExtractVideoTimestamps(string videoPath, CancellationToken cancellationToken)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=p=0 \"{videoPath}\"",
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        })!;
        var outStr = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var list = new List<double>();
        foreach (var line in outStr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(line.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                list.Add(d);
        return list;
    }

    private static List<double> MapFramesToTimestamps(int framesEmitted, List<double> allVideoTimestamps)
    {
        if (framesEmitted == 0 || allVideoTimestamps.Count == 0) return new();
        var result = new List<double>();
        
        if (allVideoTimestamps.Count < framesEmitted)
        {
            var duration = allVideoTimestamps.Last() - allVideoTimestamps.First();
            var interval = duration / (framesEmitted - 1);
            for (int i = 0; i < framesEmitted; i++)
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
        
        // 1ª tentativa: imagem original
        var result = reader.Decode(bitmap);
        if (result != null) return result;

        // 2ª tentativa: escala (melhora detecção em baixa resolução)
        result = TryScaledDecoding(reader, bitmap);
        if (result != null) return result;

        // 3ª tentativa: inversão de cores (QR claro em fundo escuro)
        result = TryInvertedDecoding(reader, bitmap);
        if (result != null) return result;

        // 4ª tentativa: alto contraste
        result = TryHighContrastDecoding(reader, bitmap);
        if (result != null) return result;

        // 5ª tentativa: binarização (preto e branco puro)
        result = TryBinarizedDecoding(reader, bitmap);
        return result;
    }

    private static ZXing.SkiaSharp.BarcodeReader CreateQrCodeReader() =>
        new()
        {
            Options = { TryHarder = true, TryInverted = true, PureBarcode = false, PossibleFormats = new[] { BarcodeFormat.QR_CODE } },
            AutoRotate = true
        };

    private static Result? TryScaledDecoding(ZXing.SkiaSharp.BarcodeReader reader, SKBitmap bitmap)
    {
        var scale = bitmap.Width < 800 ? 3 : 2;
        var scaledInfo = new SKImageInfo(bitmap.Width * scale, bitmap.Height * scale);
        using var scaledBitmap = new SKBitmap(scaledInfo);
        bitmap.ScalePixels(scaledBitmap, new SKSamplingOptions(SKFilterMode.Linear));
        return reader.Decode(scaledBitmap);
    }

    private static Result? TryInvertedDecoding(ZXing.SkiaSharp.BarcodeReader reader, SKBitmap bitmap)
    {
        using var invertedBitmap = InvertColors(bitmap);
        return invertedBitmap != null ? reader.Decode(invertedBitmap) : null;
    }

    private static Result? TryHighContrastDecoding(ZXing.SkiaSharp.BarcodeReader reader, SKBitmap bitmap)
    {
        using var contrastBitmap = HighContrast(bitmap);
        return contrastBitmap != null ? reader.Decode(contrastBitmap) : null;
    }

    private static Result? TryBinarizedDecoding(ZXing.SkiaSharp.BarcodeReader reader, SKBitmap bitmap)
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

    private string GetPartVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin.part");
    private string GetCompleteVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        return result.Trim();
    }
}