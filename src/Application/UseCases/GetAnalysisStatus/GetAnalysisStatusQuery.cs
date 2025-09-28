using Domain.Common;
using MediatR;

namespace Application.UseCases.GetAnalysisStatus;

public record GetAnalysisStatusQuery(string VideoId) 
    : IRequest<Result<GetAnalysisStatusResult>>;