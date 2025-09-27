using Application.Videos.Ports;
using Domain.Common;
using Domain.Videos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SendNotifications;

public class SendNotificationsHandler(
    IEnumerable<INotificationChannel> channels,
    ILogger<SendNotificationsHandler> logger) 
    : IRequestHandler<SendNotificationsCommand, Result<SendNotificationsResponse>>
{
    public async Task<Result<SendNotificationsResponse>> Handle(SendNotificationsCommand request, CancellationToken cancellationToken)
    {
        var tasks = channels.Select(async channel =>
        {
            try
            {
                var notification = new NotificationRequest(
                    VideoId: request.VideoId,
                    Stage: (VideoProcessingStage)request.Stage,
                    ProgressPercentage: request.ProgressPercentage,
                    Message: request.Message,
                    Timestamp: request.Timestamp
                );
                
                await channel.SendNotificationAsync(notification, cancellationToken);
                
                logger.LogDebug("Notification sent via {ChannelName} for video {VideoId}", 
                    channel.ChannelName, notification.VideoId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification via {ChannelName} for video {VideoId}", 
                    channel.ChannelName, request.VideoId);
            }
        });

        await Task.WhenAll(tasks);
        
        return new SendNotificationsResponse();
    }
}