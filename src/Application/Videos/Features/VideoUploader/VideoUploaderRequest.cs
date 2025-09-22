using MediatR;

namespace Application.Videos.Features.VideoUploader;

public record VideoUploaderRequest(string VideoId, long TotalBytes, Stream Source) : IRequest;