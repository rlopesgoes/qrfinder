using Domain.Common;
using Domain.Videos;
using MediatR;

namespace Application.UseCases.SendNotifications;

public record SendNotificationsCommand(
    string VideoId,
    VideoProcessingStage Stage,
    double ProgressPercentage,
    string? Message = null,
    DateTime Timestamp = default) : IRequest<Result<SendNotificationsResponse>>;
