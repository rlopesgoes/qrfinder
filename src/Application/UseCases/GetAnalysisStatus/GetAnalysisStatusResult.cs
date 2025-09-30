namespace Application.UseCases.GetAnalysisStatus;

public record GetAnalysisStatusResult(string Status, DateTimeOffset LastUpdatedAt);