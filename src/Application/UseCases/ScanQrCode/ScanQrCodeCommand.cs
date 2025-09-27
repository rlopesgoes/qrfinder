//using Application.Videos.Services;

using Domain.Common;
using MediatR;

namespace Application.Videos.UseCases.ProcessVideo;

public record ScanQrCodeCommand(string VideoId) : IRequest<Result<ScanQrCodeResponse>>;