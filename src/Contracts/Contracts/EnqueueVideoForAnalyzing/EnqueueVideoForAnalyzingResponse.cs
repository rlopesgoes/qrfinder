namespace Contracts.Contracts.EnqueueVideoForAnalyzing;

public record EnqueueVideoForAnalyzingResponse(string VideoId, DateTimeOffset EnqueuedAt);