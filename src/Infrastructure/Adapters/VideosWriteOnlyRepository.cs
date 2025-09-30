using Application.Ports;
using Azure.Storage.Blobs;
using Domain.Common;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters;

public sealed class VideosWriteOnlyRepository : IVideosWriteOnlyRepository
{
    private readonly BlobContainerClient _containerClient;

    public VideosWriteOnlyRepository(IOptions<BlobStorageOptions> options)
    {
        var config = options.Value;
        var blobServiceClient = new BlobServiceClient(config.ConnectionString);
        
        _containerClient = blobServiceClient.GetBlobContainerClient(config.ContainerName);
    }

    public async Task<Result> DeleteAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(videoId);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value ? Result.Success() : Result.WithError($"Video blob not found for {videoId}");
        }
        catch (Exception e)
        {
            return e;
        }
    }
}