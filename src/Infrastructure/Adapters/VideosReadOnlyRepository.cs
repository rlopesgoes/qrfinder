using Application.Ports;
using Azure.Storage.Blobs;
using Domain.Common;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters;

public sealed class VideosReadOnlyRepository : IVideosReadOnlyRepository
{
    private readonly BlobContainerClient _containerClient;

    public VideosReadOnlyRepository(IOptions<BlobStorageOptions> options)
    {
        var config = options.Value;
        var blobServiceClient = new BlobServiceClient(config.ConnectionString);
        
        _containerClient = blobServiceClient.GetBlobContainerClient(config.ContainerName);
    }

    public async Task<Result> GetIntoLocalFolderAsync(string videoId, string outputPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(videoId);

        if (!await blobClient.ExistsAsync(cancellationToken))
            return Result.WithError($"Video blob not found for {videoId}");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        if (File.Exists(outputPath))
            File.Delete(outputPath);
        
        await blobClient.DownloadToAsync(outputPath, cancellationToken);
        
        var fileInfo = new FileInfo(outputPath);

        if (fileInfo.Length is 0)
        {
            File.Delete(outputPath);
            return Result.WithError($"Downloaded file is empty for video {videoId}");
        }
        
        return Result.Success();
    }

    public async Task<Result<Stream>> GetAsync(string videoId, string localVideoPath, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(videoId);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return Result<Stream>.NoContent();

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        
            if (!response.HasValue)
                return Result<Stream>.NoContent();
        
            return response.Value.Content;
        }
        catch (Exception e)
        {
            return e;
        }
    }
}