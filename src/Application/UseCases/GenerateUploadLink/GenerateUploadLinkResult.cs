namespace Application.UseCases.GenerateUploadLink;

public record GenerateUploadLinkResult(string VideoId, string UploadUrl, DateTimeOffset ExpiresAt);