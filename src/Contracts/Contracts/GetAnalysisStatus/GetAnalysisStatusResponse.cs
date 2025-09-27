namespace Contracts.Contracts.GetAnalysisStatus;

public record GetAnalysisStatusResponse(string Status, DateTimeOffset LastUpdatedAt);