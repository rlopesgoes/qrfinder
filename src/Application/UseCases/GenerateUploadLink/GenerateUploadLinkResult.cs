namespace Application.UseCases;

public record GenerateUploadLinkResult(string VideoId, string UploadUrl, DateTimeOffset ExpiresAt);