namespace Contracts.Contracts.GenerateUploadLink;

public record GenerateUploadLinkResponse(string VideoId, string Url, DateTimeOffset ExpiresAt);