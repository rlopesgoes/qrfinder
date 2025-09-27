namespace Application.UseCases;

public record EnqueueVideoForAnalyzingResult(string VideoId, DateTimeOffset EnqueuedAt);