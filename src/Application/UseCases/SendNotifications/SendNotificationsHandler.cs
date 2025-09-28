using Application.Ports;
using Domain.Common;
using Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.SendNotifications;

public class SendNotificationsHandler(
    IEnumerable<INotificationChannel> channels) 
    : IRequestHandler<SendNotificationsCommand, Result<SendNotificationsResult>>
{
    public async Task<Result<SendNotificationsResult>> Handle(SendNotificationsCommand request, CancellationToken cancellationToken)
    {
        var tasks = channels.Select(async channel =>
        {
            var notification = new Notification(
                VideoId: request.VideoId,
                Stage: request.Stage,
                ProgressPercentage: request.ProgressPercentage,
                Message: request.Message,
                Timestamp: request.Timestamp
            );
                
            await channel.SendNotificationAsync(notification, cancellationToken);
        });

        await Task.WhenAll(tasks);
        
        return new SendNotificationsResult();
    }
}