using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Application.Videos.Data;
using Application.Videos.Data.Dto;
using Confluent.Kafka;
using Domain;
using ZXing;
using ZXing.SkiaSharp;
using SkiaSharp;

namespace Worker;

public class QrCodeScanProcessor(
    IConsumer<string, byte[]> consumer,
    IProducer<string, byte[]> producer,
    IVideoStatusRepository videoStatusRepository)
    : BackgroundService
{
    private readonly string _topicChunks = "videos.raw-chunks";
    private readonly string _topicControl = "videos.control";
    private readonly string _topicResults = "videos.results";
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
    private const int ChunkBufferSize = 64 * 1024;
    private const int DefaultFrameRate = 5;
    private const double FrameInterval = 0.2;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe([_topicChunks, _topicControl]);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? consumeResult = null;

            try
            {
                consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult is null)
                    continue;

                var topic = consumeResult.Topic!;
                var videoId = consumeResult.Message.Key!;

                if (topic == _topicChunks)
                    await AppendChunkAsync(videoId, consumeResult.Message.Value, stoppingToken);
                else if (topic == _topicControl)
                    await HandleControlAsync(videoId, consumeResult.Message.Headers, stoppingToken);

                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(50, stoppingToken);
            }
        }
    }

    private async Task AppendChunkAsync(string videoId, byte[] chunk, CancellationToken cancellationToken)
    {
        var partPath = BuildPartVideoPath(videoId);
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);

        await using var fileStream = new FileStream(
            path: partPath, 
            mode: FileMode.Append, 
            access: FileAccess.Write, 
            share: FileShare.Read,
            bufferSize: ChunkBufferSize, 
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        
        await fileStream.WriteAsync(chunk.AsMemory(), cancellationToken);
    }
    
    private const string Started = "started";
    private const string Completed = "completed";

    private async Task HandleControlAsync(string videoId, Headers headers, CancellationToken cancellationToken)
    {
        var type = headers.GetUtf8("type");
        
        if (type is null)
            return;
        
        if (type is Started)
            return;

        if (type is Completed)
        {
            await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processing), cancellationToken);
            
            var completeVideoPath = GetCompleteVideoPath(videoId);
            
            var startTime = DateTimeOffset.UtcNow;

            var detections = await DetectAsync(completeVideoPath, cancellationToken);
            
            var payload = SerializeResult(videoId, startTime, detections);
            
            await SendResultsAsync(videoId, payload, cancellationToken);
            
            await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processed), cancellationToken);

            DeleteVideo(completeVideoPath);
        }
    }

    private static byte[] SerializeResult(
        string videoId, 
        DateTimeOffset startTime,
        IReadOnlyCollection<(string text, double tSec)> detections) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            videoId,
            completedAt = DateTimeOffset.UtcNow,
            processingTimeMs = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds,
            codes = detections.Select(detection => new
            { 
                text = detection.text, 
                timestamp = TimeSpan.FromSeconds(detection.tSec).ToString(@"mm\:ss\.fff")
            }).ToArray()
        });
    
    private async Task SendResultsAsync(string videoId, byte[] payload, CancellationToken cancellationToken)
        => await producer.ProduceAsync(_topicResults, 
            new Message<string, byte[]> { Key = videoId, Value = payload }, cancellationToken);
    
    private string BuildPartVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin.part");
    private string BuildCompleteVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        
        foreach (var c in Path.GetInvalidFileNameChars()) 
            result = result.Replace(c, '_');
        
        return result.Trim();
    }

    private string GetCompleteVideoPath(string videoId)
    {
        var partPath = BuildPartVideoPath(videoId);
        var finalPath  = BuildCompleteVideoPath(videoId);
        
        if (!File.Exists(partPath))
            return finalPath;
        
        if (File.Exists(finalPath))
            File.Delete(finalPath);
        
        File.Move(partPath, finalPath);
        
        return finalPath;
    }

    private static void DeleteVideo(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
    
    private static string BuildFramesDirectory(string videoPath)
        => Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");

    private async Task<IReadOnlyList<(string text, double tSec)>> DetectAsync(string videoPath, CancellationToken cancellationToken)
    {
        var framesDirectory = BuildFramesDirectory(videoPath);
        Directory.CreateDirectory(framesDirectory);
    
        var pattern = Path.Combine(framesDirectory, "frame-%06d.png");
        
        await Run("ffmpeg", $"-y -i \"{videoPath}\" -vf \"fps={DefaultFrameRate}\" \"{pattern}\"", cancellationToken);
    
        var videoTimestamps = await ExtractVideoTimestamps(videoPath, cancellationToken);
    
        var files = Directory.GetFiles(framesDirectory, "frame-*.png")
                                    .OrderBy(x => x).ToArray();
        
        var frameTimestamps = MapFramesToTimestamps(files.Length, videoTimestamps);
    
        var reader = new BarcodeReaderGeneric
        {
            Options =
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            },
            AutoRotate = true
        };

        var results = new List<(string, double)>();
        
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
                    // Skip corrupted or unreadable frames
                }
                
                return ValueTask.CompletedTask;
            });
            
        results.AddRange(frameResults.ToList());
        
        var uniqueResults = results
            .GroupBy(r => r.Item1)
            .Select(g => g.OrderBy(r => r.Item2).First())
            .OrderBy(r => r.Item2)
            .ToList();
        
        TryDelete(framesDirectory);
        
        return uniqueResults;
    }

    private static void TryDelete(string directory)
    {
        try 
        { 
            Directory.Delete(directory, true); 
        } 
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            // Ignore cleanup errors
        }
    }

    private static async Task Run(string exe, string args, CancellationToken cancellationToken)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe, Arguments = args,
            RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false
        };
        
        using var process = System.Diagnostics.Process.Start(processStartInfo)!;
        _ = await process.StandardError.ReadToEndAsync(cancellationToken);
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0) 
            throw new Exception($"{exe} failed");
    }

    private static async Task<IReadOnlyCollection<double>> ExtractVideoTimestamps(string videoPath, CancellationToken cancellationToken)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=p=0 \"{videoPath}\"",
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        
        using var process = System.Diagnostics.Process.Start(processStartInfo)!;
        
        var outStr = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        
        await process.WaitForExitAsync(cancellationToken);

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
            if (allVideoTimestamps.Count == 0) return result;
            
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
        
        if (bitmap is null) 
            return null;

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
            Options =
            {
                TryHarder = true,
                TryInverted = true,
                PureBarcode = false,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            },
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