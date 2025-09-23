using Application.Videos.Ports;
using Domain.Videos;

namespace Infrastructure.Videos;

public class FileVideoChunkStorage : IVideoChunkStorage
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");
    private const int ChunkBufferSize = 64 * 1024;

    public FileVideoChunkStorage()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task StoreChunkAsync(VideoId videoId, byte[] chunkData, CancellationToken cancellationToken = default)
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

        await fileStream.WriteAsync(chunkData.AsMemory(), cancellationToken);
    }

    private string GetPartVideoPath(VideoId videoId) => 
        Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId.ToString())}.bin.part");

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        return result.Trim();
    }
}