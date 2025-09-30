using Application.Ports;
using Domain.Common;
using MediatR;

namespace Application.UseCases.GetAnalysisStatus;

public class GetAnalysisStatusHandler(
    IStatusReadOnlyRepository statusReadOnlyRepository) 
    : IRequestHandler<GetAnalysisStatusQuery, Result<GetAnalysisStatusResult>>
{
    public async Task<Result<GetAnalysisStatusResult>> Handle(GetAnalysisStatusQuery request, CancellationToken cancellationToken)
    {
        var analysisStatusResult = await statusReadOnlyRepository.GetAsync(request.VideoId, cancellationToken);
        if (!analysisStatusResult.IsSuccess)
            return Result<GetAnalysisStatusResult>.FromResult(analysisStatusResult);
        
        return new GetAnalysisStatusResult(
            analysisStatusResult.Value!.Stage.ToString(), 
            analysisStatusResult.Value!.UpdatedAtUtc!.Value);
        
    }
}