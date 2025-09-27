namespace Application.UseCases.EnqueueVideoForAnalyzing;

public record EnqueueVideoForAnalyzingResult(string VideoId, DateTimeOffset EnqueuedAt);