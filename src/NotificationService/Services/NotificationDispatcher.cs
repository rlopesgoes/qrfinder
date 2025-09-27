// using NotificationService.Models;
//
// namespace NotificationService.Services;
//
// /// <summary>
// /// Dispatches notifications to multiple channels
// /// </summary>
// public class NotificationDispatcher(IEnumerable<INotificationChannel> channels, ILogger<NotificationDispatcher> logger)
// {
//     public async Task DispatchAsync(NotificationRequest notification, CancellationToken cancellationToken = default)
//     {
//         var tasks = channels.Select(async channel =>
//         {
//             try
//             {
//                 await channel.SendNotificationAsync(notification, cancellationToken);
//                 
//                 logger.LogDebug("Notification sent via {ChannelName} for video {VideoId}", 
//                     channel.ChannelName, notification.VideoId);
//             }
//             catch (Exception ex)
//             {
//                 logger.LogError(ex, "Failed to send notification via {ChannelName} for video {VideoId}", 
//                     channel.ChannelName, notification.VideoId);
//             }
//         });
//
//         await Task.WhenAll(tasks);
//     }
// }