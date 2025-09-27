using Application.Videos.Ports;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly TimeSpan _expiryTime;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var config = options.Value;
        var blobServiceClient = new BlobServiceClient(config.ConnectionString);
        
        _expiryTime = TimeSpan.FromMinutes(config.MinutesToExpire);
        _containerClient = blobServiceClient.GetBlobContainerClient(config.ContainerName);
    }


    public async Task DeleteVideoAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(videoId);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> VideoExistsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(videoId);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<string> GenerateUploadUrlAsync(string videoId, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = _containerClient.GetBlobClient(videoId);
        
        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Create | BlobSasPermissions.Write,
            DateTimeOffset.UtcNow.Add(_expiryTime));
        
        return sasUri.ToString();
    }


    public async Task<string> DownloadVideoDirectlyAsync(string videoId, string outputPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(videoId);
        
        if (!await blobClient.ExistsAsync(cancellationToken))
            throw new FileNotFoundException($"Video blob not found for {videoId}");

        // Get blob properties to check size
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        Console.WriteLine($"[DEBUG] Blob {videoId} exists. Size: {properties.Value.ContentLength} bytes");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        Console.WriteLine($"[DEBUG] Downloading blob {videoId} to {outputPath}");
        await blobClient.DownloadToAsync(outputPath, cancellationToken);
        
        var fileInfo = new FileInfo(outputPath);
        Console.WriteLine($"[DEBUG] Downloaded file size: {fileInfo.Length} bytes");
        
        if (fileInfo.Length == 0)
            throw new InvalidOperationException($"Downloaded file is empty for video {videoId}");
        
        return outputPath;
    }
}