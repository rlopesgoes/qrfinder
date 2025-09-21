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
        
        consumer.Subscribe(new[] { _topicChunks, _topicControl });

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? cr = null;
            try
            {
                cr = consumer.Consume(stoppingToken);
                if (cr is null) continue;

                var topic = cr.Topic;
                var videoId = cr.Message.Key!;


                await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processing), stoppingToken);
                
                if (topic == _topicChunks)
                {
                    await AppendChunkAsync(videoId, cr.Message.Value, stoppingToken);
                }
                else if (topic == _topicControl)
                {
                    await HandleControlAsync(videoId, cr.Message.Headers, stoppingToken);
                }
                
                consumer.Commit(cr); 
            }
            catch (OperationCanceledException) { break; }
            catch (Exception)
            {
                await Task.Delay(50, stoppingToken);
            }
        }
    }

    private async Task AppendChunkAsync(string videoId, byte[] chunk, CancellationToken ct)
    {
        var partPath = PartPath(videoId);
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);

        await using var fs = new FileStream(partPath, FileMode.Append, FileAccess.Write, FileShare.Read,
            bufferSize: ChunkBufferSize, options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await fs.WriteAsync(chunk.AsMemory(), ct);

    }

    private async Task HandleControlAsync(string videoId, Headers headers, CancellationToken ct)
    {
        var type = headers.GetUtf8("type") ?? "";
        if (type == "started")
        {
            return;
        }

        if (type == "completed")
        {
            var lastSeq = headers.GetInt64("lastSeq");
            var total = headers.GetInt64("total-bytes");
            var fin = await FinalizeAsync(videoId);

            var detections = await DetectAsync(fin, ct);
            
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                videoId,
                completedAt = DateTimeOffset.UtcNow,
                processingTimeMs = DateTimeOffset.UtcNow.Subtract(DateTimeOffset.UtcNow).TotalMilliseconds,
                totalFramesProcessed = detections.Count > 0 ? "Multiple" : "18",
                codes = detections.Select(d => new { 
                    text = d.text, 
                    timestampSeconds = Math.Round(d.tSec, 3),
                    formattedTime = TimeSpan.FromSeconds(d.tSec).ToString(@"mm\:ss\.fff")
                }).ToArray()
            });
            
            await producer.ProduceAsync(_topicResults,
                new Message<string, byte[]> { Key = videoId, Value = payload }, ct);
            
            
            await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processed), ct);

            TryDelete(fin);
        }
    }

    private string PartPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin.part");
    private string FinalPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");

    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
        return fileName.Trim();
    }

    private async Task<string> FinalizeAsync(string videoId)
    {
        var part = PartPath(videoId);
        var fin  = FinalPath(videoId);
        if (!File.Exists(part))
        {
                return fin;
        }
        if (File.Exists(fin)) File.Delete(fin);
        File.Move(part, fin);
        await Task.Yield();
        return fin;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public async Task<IReadOnlyList<(string text, double tSec)>> DetectAsync(string videoPath, CancellationToken ct)
    {
        if (!await IsCommandAvailable("ffmpeg"))
        {
            throw new InvalidOperationException("FFmpeg not found. Install with: brew install ffmpeg (macOS) or apt install ffmpeg (Linux)");
        }
        
        if (!await IsCommandAvailable("ffprobe"))
        {
            throw new InvalidOperationException("FFprobe not found. Install with: brew install ffmpeg (macOS) or apt install ffmpeg (Linux)");
        }

        var dir = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");
        Directory.CreateDirectory(dir);
    
        var pattern = Path.Combine(dir, "frame-%06d.png");
        await Run("ffmpeg", $"-y -i \"{videoPath}\" -vf \"fps={DefaultFrameRate}\" \"{pattern}\"", ct);
        
        var extractedFiles = Directory.GetFiles(dir, "frame-*.png");
    
        var pts = await ProbePts(videoPath, ct);
    
        var files = Directory.GetFiles(dir, "frame-*.png").OrderBy(x => x).ToArray();
        var tList = MapFramesToPts(files.Length, pts);
    
        var reader = new BarcodeReaderGeneric();
        reader.Options.TryHarder = true;
        reader.Options.TryInverted = true;
        reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
        reader.AutoRotate = true;
    
        var results = new List<(string, double)>();
        
        var lockResults = new object();
        
        await Parallel.ForEachAsync(files.Select((file, index) => new { file, index }), 
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            },
            (item, _) =>
            {
                try
                {
                    var result = ProcessFrame(item.file);
                    if (result != null)
                    {
                        lock (lockResults)
                        {
                            var timestamp = item.index < tList.Count ? tList[item.index] : (item.index * FrameInterval);
                            results.Add((result.Text, timestamp));
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore frame processing errors
                }
                
                return ValueTask.CompletedTask;
            });
    
        
        var uniqueResults = results
            .GroupBy(r => r.Item1)
            .Select(g => g.OrderBy(r => r.Item2).First())
            .OrderBy(r => r.Item2)
            .ToList();
            
        
        try { Directory.Delete(dir, true); } catch { }
        return uniqueResults;
    }

    private static async Task Run(string exe, string args, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe, Arguments = args,
            RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        _ = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new Exception($"{exe} failed");
    }

    private static async Task<List<double>> ProbePts(string videoPath, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=p=0 \"{videoPath}\"",
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        var outStr = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);

        var list = new List<double>();
        foreach (var line in outStr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(line.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                list.Add(d);
        return list;
    }

    private static List<double> MapFramesToPts(int framesEmitted, List<double> allPts)
    {
        if (framesEmitted == 0 || allPts.Count == 0) return new();
        
        var result = new List<double>();
        
        if (allPts.Count < framesEmitted)
        {
            if (allPts.Count == 0) return result;
            
            var duration = allPts.Last() - allPts.First();
            var interval = duration / (framesEmitted - 1);
            
            for (int i = 0; i < framesEmitted; i++)
            {
                result.Add(allPts.First() + (i * interval));
            }
            return result;
        }
        
        var videoDuration = allPts.Last() - allPts.First();
        var frameInterval = videoDuration / (framesEmitted - 1);
        
        for (int i = 0; i < framesEmitted; i++)
        {
            var targetTime = allPts.First() + (i * frameInterval);
            
            var closestPts = allPts.OrderBy(pts => Math.Abs(pts - targetTime)).First();
            result.Add(closestPts);
        }
        
        return result;
    }

    private static async Task<bool> IsCommandAvailable(string command)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return false;
            
            await p.WaitForExitAsync();
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private Result? ProcessFrame(string filePath)
    {
        using var bitmap = SKBitmap.Decode(filePath);
        if (bitmap == null) return null;

        var reader = CreateQrCodeReader();
        
        var result = reader.Decode(bitmap);
        if (result != null) return result;

        result = TryScaledDecoding(reader, bitmap);
        if (result != null) return result;

        result = TryInvertedDecoding(reader, bitmap);
        if (result != null) return result;

        result = TryHighContrastDecoding(reader, bitmap);
        if (result != null) return result;

        result = TryBinarizedDecoding(reader, bitmap);
        return result;
    }

    private static ZXing.SkiaSharp.BarcodeReader CreateQrCodeReader() =>
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