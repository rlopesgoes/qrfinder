using Application.Videos.Ports;

namespace Infrastructure;

public class VideoFileService : IVideoStorageService
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
    private const int ChunkBufferSize = 64 * 1024;

    public async Task StoreVideoPartAsync(string videoId, byte[] videoPart, CancellationToken cancellationToken)
    {
        var partPath = GetPartVideoPath(videoId);
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);

        await using var fileStream = new FileStream(
            path: partPath,
            mode: FileMode.Append,
            access: FileAccess.Write,
            share: FileShare.Read,
            bufferSize: ChunkBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await fileStream.WriteAsync(videoPart.AsMemory(), cancellationToken);
    }

    private string GetPartVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin.part");

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        
        return result.Trim();
    }
}