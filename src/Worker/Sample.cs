using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using ZXing;

namespace Worker;

public class Sample(
    IConsumer<string, byte[]> consumer,
    IProducer<string, byte[]> producer)
    : BackgroundService
{
    private string topicChunks = "videos.raw-chunks";
    private string topicControl =  "videos.control";
    private string topicResults =  "videos.results";
    private string dir = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicChunks = "videos.raw-chunks";
        var topicControl =  "videos.control";
        var topicResults =  "videos.results";
        var dir = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
        
        consumer.Subscribe(new[] { topicChunks, topicControl });

        Console.WriteLine($"[Worker] Subscribed to: {topicChunks}, {topicControl}. Results -> {topicResults}");
        //Console.WriteLine($"[Worker] Dir: {dir}");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? cr = null;
            try
            {
                cr = consumer.Consume(stoppingToken);
                if (cr is null) continue;

                var topic = cr.Topic;
                var videoId = cr.Message.Key!;
                if (topic == topicChunks)
                {
                    await AppendChunkAsync(videoId, cr.Message.Value, stoppingToken);
                }
                else if (topic == topicControl)
                {
                    await HandleControlAsync(videoId, cr.Message.Headers, stoppingToken);
                }
                
                consumer.Commit(cr); 
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker] erro: {ex.Message}");
                await Task.Delay(50, stoppingToken);
            }
        }
    }

    // ----------------- CHUNKS -----------------
    private async Task AppendChunkAsync(string videoId, byte[] chunk, CancellationToken ct)
    {
        var partPath = PartPath(videoId);
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);

        await using var fs = new FileStream(partPath, FileMode.Append, FileAccess.Write, FileShare.Read,
            bufferSize: 64 * 1024, options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await fs.WriteAsync(chunk.AsMemory(), ct);

        Console.WriteLine($"[Chunk] {videoId} +{chunk.Length} -> {new FileInfo(partPath).Length} bytes");
    }

    // --------------- CONTROLS -----------------
    private async Task HandleControlAsync(string videoId, Headers headers, CancellationToken ct)
    {
        var type = headers.GetUtf8("type") ?? "";
        if (type == "started")
        {
            Console.WriteLine($"[Control] started: {videoId}");
            return;
        }

        if (type == "completed")
        {
            // Pode vir header lastSeq e/ou totalBytes (opcional)
            var lastSeq  = headers.GetInt64("lastSeq");
            var total    = headers.GetInt64("total-bytes");
            Console.WriteLine($"[Control] completed: {videoId}, lastSeq={lastSeq?.ToString() ?? "-"}, total={total?.ToString() ?? "-"}");

            // finalize .part -> .bin
            var fin = await FinalizeAsync(videoId);

           // processa (ffmpeg + zxing)
            var detections = await DetectAsync(fin, ct);
            
            // publica em videos.results
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                videoId,
                completedAt = DateTimeOffset.UtcNow,
                codes = detections.Select(d => new { text = d.text, tSec = d.tSec }).ToArray()
            });
            
            await producer.ProduceAsync(topicResults,
                new Message<string, byte[]> { Key = videoId, Value = payload }, ct);
            
            Console.WriteLine($"[Results] {videoId} -> {detections.Count} códigos");

            // limpeza
            TryDelete(fin);
        }
    }

    // --------------- FILE HELPERS ---------------
    private string PartPath(string videoId) => Path.Combine(dir, $"{San(videoId)}.bin.part");
    private string FinalPath(string videoId) => Path.Combine(dir, $"{San(videoId)}.bin");

    private static string San(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }

    private async Task<string> FinalizeAsync(string videoId)
    {
        var part = PartPath(videoId);
        var fin  = FinalPath(videoId);
        if (!File.Exists(part))
        {
            Console.WriteLine($"[Warn] .part não encontrado para {videoId}");
            return fin;
        }
        if (File.Exists(fin)) File.Delete(fin);
        File.Move(part, fin);
        Console.WriteLine($"[Finalize] {videoId}: {new FileInfo(fin).Length} bytes");
        await Task.Yield();
        return fin;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    // --------------- DETECTOR -------------------
    public async Task<IReadOnlyList<(string text, double tSec)>> DetectAsync(string videoPath, CancellationToken ct)
    {
        var dir = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");
        Directory.CreateDirectory(dir);
    
        // 1) extrai 2 fps
        var pattern = Path.Combine(dir, "frame-%06d.png");
        await Run("ffmpeg", $"-y -threads 1 -i \"{videoPath}\" -vf fps=2 \"{pattern}\"", ct);
    
        // 2) PTS de todos os frames do stream
        var pts = await ProbePts(videoPath, ct);
    
        // 3) relaciona frames gerados (2fps) com PTS
        var files = Directory.GetFiles(dir, "frame-*.png").OrderBy(x => x).ToArray();
        var tList = MapFramesToPts(files.Length, pts);
    
        var reader = new ZXing.BarcodeReader   // <<--- este aqui, não generic
        {
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new() { BarcodeFormat.QR_CODE }
            },
            AutoRotate = true,
            TryInverted = true
        };
    
        var results = new List<(string, double)>();
        for (int i = 0; i < files.Length; i++)
        {
            using var bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(files[i]);
            var r = reader.Decode(bmp);   // funciona com Bitmap
            if (r is not null && !string.IsNullOrWhiteSpace(r.Text))
                results.Add((r.Text, i < tList.Count ? tList[i] : i / 2.0));
        }
    
        try { Directory.Delete(dir, true); } catch { /* ignore */ }
        return results;
    }

    private static async Task Run(string exe, string args, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe, Arguments = args,
            RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        _ = await p.StandardError.ReadToEndAsync(ct); // ffmpeg fala no stderr
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
        var step = Math.Max(1, allPts.Count / framesEmitted);
        return allPts.Where((_, i) => i % step == 0).Take(framesEmitted).ToList();
    }
}

// --------------- Headers helpers ---------------
static class HeaderExt
{
    public static string? GetUtf8(this Headers h, string key)
    {
        var hdr = h.LastOrDefault(x => x.Key == key);
        if (hdr is null) return null;
        return System.Text.Encoding.UTF8.GetString(hdr.GetValueBytes());
    }

    public static long? GetInt64(this Headers h, string key)
    {
        var hdr = h.LastOrDefault(x => x.Key == key);
        if (hdr is null) return null;
        var b = hdr.GetValueBytes();
        return b.Length switch
        {
            8 => BitConverter.ToInt64(b),
            4 => BitConverter.ToInt32(b),
            _ => null
        };
    }
}
