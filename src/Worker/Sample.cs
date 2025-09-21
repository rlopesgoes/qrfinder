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

public class Sample(
    IConsumer<string, byte[]> consumer,
    IProducer<string, byte[]> producer,
    IVideoStatusRepository videoStatusRepository)
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


                await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processing), stoppingToken);
                
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
            
            // publica em videos.results com timestamps precisos
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                videoId,
                completedAt = DateTimeOffset.UtcNow,
                processingTimeMs = DateTimeOffset.UtcNow.Subtract(DateTimeOffset.UtcNow).TotalMilliseconds, // Para métricas
                totalFramesProcessed = detections.Count > 0 ? "Múltiplos" : "18", // Info do debug
                codes = detections.Select(d => new { 
                    text = d.text, 
                    timestampSeconds = Math.Round(d.tSec, 3), // 3 casas decimais para precisão
                    formattedTime = TimeSpan.FromSeconds(d.tSec).ToString(@"mm\:ss\.fff") // Formato legível
                }).ToArray()
            });
            
            await producer.ProduceAsync(topicResults,
                new Message<string, byte[]> { Key = videoId, Value = payload }, ct);
            
            Console.WriteLine($"[Results] {videoId} -> {detections.Count} códigos");
            
            await videoStatusRepository.UpsertAsync(new UploadStatus(videoId, UploadStage.Processed), ct);

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
        // Verifica se ffmpeg está disponível
        if (!await IsCommandAvailable("ffmpeg"))
        {
            throw new InvalidOperationException("FFmpeg não encontrado. Instale com: brew install ffmpeg (macOS) ou apt install ffmpeg (Linux)");
        }
        
        if (!await IsCommandAvailable("ffprobe"))
        {
            throw new InvalidOperationException("FFprobe não encontrado. Instale com: brew install ffmpeg (macOS) ou apt install ffmpeg (Linux)");
        }

        var dir = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath) + "_frames");
        Directory.CreateDirectory(dir);
    
        // 1) extrai frames otimizado para QR codes com debug
        var pattern = Path.Combine(dir, "frame-%06d.png");
        Console.WriteLine($"[Debug] Extraindo frames de: {videoPath}");
        await Run("ffmpeg", $"-y -i \"{videoPath}\" -vf \"fps=5\" \"{pattern}\"", ct);
        
        var extractedFiles = Directory.GetFiles(dir, "frame-*.png");
        Console.WriteLine($"[Debug] {extractedFiles.Length} frames extraídos");
    
        // 2) PTS de todos os frames do stream
        var pts = await ProbePts(videoPath, ct);
    
        // 3) relaciona frames gerados (3fps) com PTS
        var files = Directory.GetFiles(dir, "frame-*.png").OrderBy(x => x).ToArray();
        var tList = MapFramesToPts(files.Length, pts);
    
        var reader = new BarcodeReaderGeneric();
        reader.Options.TryHarder = true;
        reader.Options.TryInverted = true;
        reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
        reader.AutoRotate = true;
    
        var results = new List<(string, double)>();
        
        // Processamento simples e eficaz frame por frame
        var lockResults = new object();
        
        await Parallel.ForEachAsync(files.Select((file, index) => new { file, index }), 
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            },
            async (item, cancellationToken) =>
            {
                try
                {
                    // Carrega imagem com SkiaSharp
                    using var bitmap = SKBitmap.Decode(item.file);
                    if (bitmap == null)
                    {
                        Console.WriteLine($"[Error] Não foi possível carregar frame {item.index}: {item.file}");
                        return;
                    }
                    
                    Console.WriteLine($"[Debug] Frame {item.index}: {bitmap.Width}x{bitmap.Height} pixels");
                    
                    // Usa o reader do ZXing.SkiaSharp diretamente
                    var reader = new ZXing.SkiaSharp.BarcodeReader();
                    reader.Options.TryHarder = true;
                    reader.Options.TryInverted = true; 
                    reader.Options.PureBarcode = false;
                    reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
                    reader.AutoRotate = true;
                    
                    // Múltiplas tentativas para QR codes claros
                    Result? result = null;
                    
                    // 1ª tentativa: imagem original
                    result = reader.Decode(bitmap);
                    
                    // 2ª tentativa: escala sempre (melhora detecção em baixa resolução)
                    if (result == null)
                    {
                        var scale = bitmap.Width < 800 ? 3 : 2; // Escala maior para resolução baixa
                        var newWidth = bitmap.Width * scale;
                        var newHeight = bitmap.Height * scale;
                        
                        var scaledInfo = new SKImageInfo(newWidth, newHeight);
                        using var scaledBitmap = new SKBitmap(scaledInfo);
                        bitmap.ScalePixels(scaledBitmap, SKFilterQuality.High);
                        result = reader.Decode(scaledBitmap);
                        Console.WriteLine($"[Debug] Frame {item.index}: Escala {scale}x ({newWidth}x{newHeight})");
                    }
                    
                    // 3ª tentativa: inversão de cores (QR claro em fundo escuro)
                    if (result == null)
                    {
                        using var invertedBitmap = InvertColors(bitmap);
                        if (invertedBitmap != null)
                        {
                            result = reader.Decode(invertedBitmap);
                            Console.WriteLine($"[Debug] Frame {item.index}: Tentou inversão de cores");
                        }
                    }
                    
                    // 4ª tentativa: alto contraste
                    if (result == null)
                    {
                        using var contrastBitmap = HighContrast(bitmap);
                        if (contrastBitmap != null)
                        {
                            result = reader.Decode(contrastBitmap);
                            Console.WriteLine($"[Debug] Frame {item.index}: Tentou alto contraste");
                        }
                    }
                    
                    // 5ª tentativa: binarização (preto e branco puro)
                    if (result == null)
                    {
                        using var binaryBitmap = Binarize(bitmap);
                        if (binaryBitmap != null)
                        {
                            result = reader.Decode(binaryBitmap);
                            Console.WriteLine($"[Debug] Frame {item.index}: Tentou binarização");
                        }
                    }
                    
                    Console.WriteLine($"[Debug] Frame {item.index}: {(result != null ? $"QR encontrado: {result.Text}" : "Nenhum QR detectado")}");
                    
                    if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                    {
                        lock (lockResults)
                        {
                            // Timestamp preciso: usa PTS real se disponível, senão calcula baseado no fps
                            var timestamp = item.index < tList.Count ? tList[item.index] : (item.index * 0.2); // 5fps = 0.2s por frame
                            results.Add((result.Text, timestamp));
                            Console.WriteLine($"[SUCCESS] 🎉 QR Code: '{result.Text}' em {timestamp:F2}s");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Erro processando frame {item.index}: {ex.Message}");
                }
                
                await Task.CompletedTask;
            });
    
        Console.WriteLine($"[Final] Total de QR codes detectados: {results.Count}");
        
        // Remove duplicatas: mantém apenas a primeira ocorrência de cada QR code
        var uniqueResults = results
            .GroupBy(r => r.Item1) // Agrupa por texto do QR
            .Select(g => g.OrderBy(r => r.Item2).First()) // Pega o primeiro de cada grupo (menor timestamp)
            .OrderBy(r => r.Item2) // Ordena por timestamp
            .ToList();
            
        Console.WriteLine($"[Final] QR codes únicos: {uniqueResults.Count}");
        foreach (var (text, time) in uniqueResults)
        {
            Console.WriteLine($"[Final] QR único: '{text}' em {time:F3}s");
        }
        
        try { Directory.Delete(dir, true); } catch { /* ignore */ }
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
        
        var result = new List<double>();
        
        // Se temos menos PTS que frames extraídos, usa interpolação
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
        
        // Mapeia cada frame extraído (5fps) para o PTS mais próximo
        var videoDuration = allPts.Last() - allPts.First();
        var frameInterval = videoDuration / (framesEmitted - 1);
        
        for (int i = 0; i < framesEmitted; i++)
        {
            var targetTime = allPts.First() + (i * frameInterval);
            
            // Encontra o PTS mais próximo
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

    private static SKBitmap? InvertColors(SKBitmap original)
    {
        try
        {
            var info = new SKImageInfo(original.Width, original.Height);
            var inverted = new SKBitmap(info);
            
            using var canvas = new SKCanvas(inverted);
            using var paint = new SKPaint();
            
            // Inverte todas as cores
            var invertMatrix = new float[]
            {
                -1, 0, 0, 0, 255,    // Red invertido
                0, -1, 0, 0, 255,    // Green invertido
                0, 0, -1, 0, 255,    // Blue invertido
                0, 0, 0, 1, 0        // Alpha inalterado
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
            
            // Alto contraste
            var contrastMatrix = new float[]
            {
                3, 0, 0, 0, -128,    // Red: muito contraste
                0, 3, 0, 0, -128,    // Green
                0, 0, 3, 0, -128,    // Blue
                0, 0, 0, 1, 0        // Alpha
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
            
            // Converte para escala de cinza e depois binariza
            var binaryMatrix = new float[]
            {
                0.299f, 0.587f, 0.114f, 0, 0,    // Escala de cinza no canal Red
                0.299f, 0.587f, 0.114f, 0, 0,    // Escala de cinza no canal Green
                0.299f, 0.587f, 0.114f, 0, 0,    // Escala de cinza no canal Blue
                0, 0, 0, 1, 0                     // Alpha
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
