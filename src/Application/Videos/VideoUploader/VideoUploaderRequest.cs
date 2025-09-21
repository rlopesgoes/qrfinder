using MediatR;

namespace Application.Videos.VideoUploader;

public record VideoUploaderRequest(string VideoId, long TotalBytes, Stream Source) : IRequest;