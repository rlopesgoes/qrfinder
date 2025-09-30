using System.Collections.Concurrent;
using System.Globalization;
using Application.Ports;
using Domain.Common;
using Domain.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;
using Result = ZXing.Result;

namespace Infrastructure.Adapters;

public class QrCodeScannerWithFFmpeg : IQrCodeScanner
{
    private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "processing");
    private const int DefaultFrameRate = 5;
    private const double FrameInterval = 0.2;
    
    private static string BuildTemporaryVideoFileName(string videoId)
        => Path.Combine(TempDirectory, $"{videoId}.mp4");
    
    public async Task<Result<QrCodes>> ScanAsync(Video video, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);
        
        var path = BuildTemporaryVideoFileName(video.Id);
        var content = video.Content;
        
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        
        var results = await DetectQrCodesInternalAsync(path, cancellationToken);
        
        if (File.Exists(path))
            File.Delete(path);

        var qrCodes = results.Select(qr => new QrCode(qr.content, new Timestamp(qr.timestamp))).ToList();
        
        return new QrCodes(qrCodes);
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
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount, 
                CancellationToken = cancellationToken
            },
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
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    
                }
                
                return ValueTask.CompletedTask;
            });

        var results = frameResults
            .Select(r => (content: r.text, timestamp: r.timestamp))
            .ToList();

        Directory.Delete(framesDirectory, true);
        
        return results;
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
        using var image = Image.Load<Rgba32>(filePath);
        if (image is null) return null;

        var reader = CreateQrCodeReader();

        // Try single decode first
        var result = reader.Decode(ConvertToLuminanceSource(image));
        if (result != null) return result;

        // Try decode multiple (if available)
        try
        {
            var results = reader.DecodeMultiple(ConvertToLuminanceSource(image));
            if (results != null && results.Length > 0)
                return results[0]; // ou processar todos
        }
        catch { /* some bindings may not support DecodeMultiple */ }

        // rest of your attempts
        result = TryScaledDecoding(reader, image);
        // ...
        return result;
    }


    private static BarcodeReaderGeneric CreateQrCodeReader() =>
        new()
        {
            Options = { TryHarder = true, TryInverted = true, PureBarcode = false, PossibleFormats = new[] { BarcodeFormat.QR_CODE } },
            AutoRotate = true
        };

    private static Result? TryScaledDecoding(BarcodeReaderGeneric reader, Image<Rgba32> image)
    {
        var scale = image.Width < 800 ? 3 : 2;
        using var scaledImage = image.Clone(ctx => ctx.Resize(image.Width * scale, image.Height * scale));
        return reader.Decode(ConvertToLuminanceSource(scaledImage));
    }

    private static Result? TryInvertedDecoding(BarcodeReaderGeneric reader, Image<Rgba32> image)
    {
        using var invertedImage = InvertColors(image);
        return invertedImage != null ? reader.Decode(ConvertToLuminanceSource(invertedImage)) : null;
    }

    private static Result? TryHighContrastDecoding(BarcodeReaderGeneric reader, Image<Rgba32> image)
    {
        using var contrastImage = HighContrast(image);
        return contrastImage != null ? reader.Decode(ConvertToLuminanceSource(contrastImage)) : null;
    }

    private static Result? TryBinarizedDecoding(BarcodeReaderGeneric reader, Image<Rgba32> image)
    {
        using var binaryImage = Binarize(image);
        return binaryImage != null ? reader.Decode(ConvertToLuminanceSource(binaryImage)) : null;
    }

    private static Image<Rgba32>? InvertColors(Image<Rgba32> original)
    {
        try
        {
            var inverted = original.Clone(ctx => ctx.Invert());
            return inverted;
        }
        catch
        {
            return null;
        }
    }

    private static Image<Rgba32>? HighContrast(Image<Rgba32> original)
    {
        try
        {
            var contrast = original.Clone(ctx => ctx.Contrast(2.0f));
            return contrast;
        }
        catch
        {
            return null;
        }
    }

    private static Image<Rgba32>? Binarize(Image<Rgba32> original)
    {
        try
        {
            var binary = original.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.5f));
            return binary;
        }
        catch
        {
            return null;
        }
    }

    private static LuminanceSource ConvertToLuminanceSource(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;

        // buffer RGB 24bpp: 3 bytes por pixel (R, G, B)
        var rgb = new byte[width * height * 3];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                int rowOffset = y * width * 3; // deslocamento em bytes para esta linha

                for (int x = 0; x < width; x++)
                {
                    var p = row[x];
                    int i = rowOffset + x * 3;
                    rgb[i + 0] = p.R;
                    rgb[i + 1] = p.G;
                    rgb[i + 2] = p.B;
                }
            }
        });

        // Construtor que aceita byte[] RGB24
        return new ZXing.RGBLuminanceSource(rgb, width, height);
    }


}