//using Application.Videos.Services;

using Domain.Common;
using MediatR;

namespace Application.UseCases.ScanQrCode;

public record ScanQrCodeCommand(string VideoId)
    : IRequest<Result<ScanQrCodeResult>>;