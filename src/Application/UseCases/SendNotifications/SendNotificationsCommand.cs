using Domain.Common;
using Domain.Models;
using MediatR;

namespace Application.UseCases.SendNotifications;

public record SendNotificationsCommand(
    string VideoId,
    Stage Stage,
    double ProgressPercentage,
    string? Message = null,
    DateTime Timestamp = default) 
    : IRequest<Result<SendNotificationsResult>>;
