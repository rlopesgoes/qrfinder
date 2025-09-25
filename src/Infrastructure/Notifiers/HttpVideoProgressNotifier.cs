using Application.Videos.Ports;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifiers;

/// <summary>
/// HTTP implementation that calls NotificationService
/// </summary>
public class HttpVideoProgressNotifier(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpVideoProgressNotifier> logger)
    : IVideoProgressNotifier
{
    public async Task NotifyProgressAsync(string videoId, string stage, double progressPercentage, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var notificationServiceUrl = configuration.GetConnectionString("NotificationService") ?? "https://localhost:7002";
            var endpoint = $"{notificationServiceUrl}/api/notifications/progress";

            var payload = new
            {
                VideoId = videoId,
                Stage = stage,
                ProgressPercentage = progressPercentage,
                ErrorMessage = errorMessage
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to notify progress for video {VideoId}: {StatusCode}", 
                    videoId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error notifying progress for video {VideoId}", videoId);
        }
    }
}