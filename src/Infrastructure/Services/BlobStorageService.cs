using Application.Videos.Ports;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private const string ContainerName = "videos";

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    public async Task UploadChunkAsync(string videoId, int chunkIndex, byte[] chunkData, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        
        var blobName = $"{videoId}/chunk_{chunkIndex:D6}";
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        using var stream = new MemoryStream(chunkData);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
    }

    public async Task<byte[]> DownloadChunkAsync(string videoId, int chunkIndex, CancellationToken cancellationToken = default)
    {
        var blobName = $"{videoId}/chunk_{chunkIndex:D6}";
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }

    public async Task<Stream> GetVideoStreamAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var chunks = await GetUploadedChunksAsync(videoId, cancellationToken);
        if (chunks.Count == 0)
            throw new FileNotFoundException($"No chunks found for video {videoId}");

        chunks.Sort();
        var memoryStream = new MemoryStream();

        foreach (var chunkIndex in chunks)
        {
            var chunkData = await DownloadChunkAsync(videoId, chunkIndex, cancellationToken);
            await memoryStream.WriteAsync(chunkData, cancellationToken);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var blobs = _containerClient.GetBlobsAsync(prefix: $"{videoId}/", cancellationToken: cancellationToken);
        
        await foreach (var blob in blobs)
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var chunks = await GetUploadedChunksAsync(videoId, cancellationToken);
        return chunks.Count > 0;
    }

    public async Task<List<int>> GetUploadedChunksAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var chunkIndices = new List<int>();
        var blobs = _containerClient.GetBlobsAsync(prefix: $"{videoId}/chunk_", cancellationToken: cancellationToken);

        await foreach (var blob in blobs)
        {
            var fileName = Path.GetFileName(blob.Name);
            if (fileName.StartsWith("chunk_") && fileName.Length >= 12) // chunk_000000
            {
                var chunkIndexStr = fileName.Substring(6, 6);
                if (int.TryParse(chunkIndexStr, out var chunkIndex))
                {
                    chunkIndices.Add(chunkIndex);
                }
            }
        }

        return chunkIndices;
    }
}