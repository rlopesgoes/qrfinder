using Application.Ports;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Domain.Common;
using Domain.Models;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters;

internal sealed class UploadLinkGenerator : IUploadLinkGenerator
{
    private readonly ILogger<UploadLinkGenerator> _logger;
    private readonly BlobContainerClient _containerClient;
    private readonly TimeSpan _expiryTime;
    private readonly string _baseUrl;

    public UploadLinkGenerator(
        ILogger<UploadLinkGenerator> logger,
        IOptions<BlobStorageOptions> options)
    {
        var config = options.Value;
        var blobServiceClient = new BlobServiceClient(config.ConnectionString);
        
        _logger = logger;
        _expiryTime = TimeSpan.FromMinutes(config.MinutesToExpire);
        _baseUrl = config.BaseUrl;
        _containerClient = blobServiceClient.GetBlobContainerClient(config.ContainerName);
    }
    
    public async Task<Result<UploadLink>> GenerateAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("ContainerAlreadyExists"))
            {
                // Container already exists, this is fine in concurrent scenarios
                _logger.LogDebug("Container already exists for video {VideoId}", videoId);
            }
            
            var blobClient = _containerClient.GetBlobClient(videoId);
            var expiresAt = DateTimeOffset.UtcNow.Add(_expiryTime);
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, expiresAt);
            
            var uri = $"{_baseUrl}{sasUri.PathAndQuery}";
            
            return new UploadLink(uri, expiresAt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to generate upload link for video {VideoId}", videoId);
            return e;
        }
    }
}