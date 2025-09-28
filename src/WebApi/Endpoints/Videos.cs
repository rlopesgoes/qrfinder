using Application.Videos.Features.GetVideoResults;
using Application.UseCases.EnqueueVideoForAnalyzing;
using Application.UseCases.GenerateUploadLink;
using Application.UseCases.GetAnalysisStatus;
using Contracts.Contracts.EnqueueVideoForAnalyzing;
using Contracts.Contracts.GenerateUploadLink;
using Contracts.Contracts.GetAnalysisStatus;
using Contracts.Contracts.GetVideoResults;
using Domain.Common;
using MediatR;

namespace WebApi.Endpoints;

public static class Videos
{
    private const string QrCodeFinder = "QR Code finder";

    public static void MapVideosEndpoints(this WebApplication app)
    {
        app.MapPost("/video/upload-link/generate", async
                (GenerateUploadLinkRequest? request, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(request.ToCommand(), cancellationToken);

                if (!result.IsSuccess)
                    return Results.Problem(
                        title: "Error on generating upload link",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);

                return Results.Ok(result.Value!.ToDto());
            })
            .WithName("Generate upload link")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapPatch("/video/{id:guid}/analyze", async
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var request = new EnqueueVideoForAnalyzingRequest(id);
                
                var result = await mediator.Send(request.ToCommand(), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value!.ToDto())
                    : Results.Problem(
                        title: $"Failed to enqueue video {id}",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
            })
            .WithName("Analyze video")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapGet("/video/{id:guid}/status",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var request = new GetAnalysisStatusRequest(id);
                
                var result = await mediator.Send(request.ToQuery(), cancellationToken);

                if (!result.IsSuccessOrNoContent)
                    Results.Problem(
                        title: $"Failed to get video video {id}",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);

                if (result.StatusCode is StatusCode.NoContent)
                    return Results.NoContent();

                return Results.Ok(result.Value!.ToDto());
            })
            .WithName("Get status of video processing")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapGet("/video/{id:guid}/results", 
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var request = new GetAnalysisResultsRequest(id);
                
                var result = await mediator.Send(request.ToQuery(), cancellationToken);
                
                if (!result.IsSuccess)
                    return Results.Problem(
                        title: $"Failed to get video result {id}",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                
                if (result.StatusCode is StatusCode.NoContent)
                    return Results.NoContent();

                return Results.Ok(result.Value!.ToDto());
            })
            .WithName("Get results of qr codes finding")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
    }
}

public static class ContractsMappers
{
    public static GenerateUploadLinkCommand ToCommand(this GenerateUploadLinkRequest? request) 
        => request is null ? new GenerateUploadLinkCommand() : new GenerateUploadLinkCommand(request.VideoId);

    public static GenerateUploadLinkResponse ToDto(this GenerateUploadLinkResult result)
        => new(result.VideoId, result.UploadUrl, result.ExpiresAt);
    
    public static EnqueueVideoForAnalyzingCommand ToCommand(this EnqueueVideoForAnalyzingRequest request) =>
        new(request.VideoId.ToString());

    public static EnqueueVideoForAnalyzingResponse ToDto(this EnqueueVideoForAnalyzingResult result)
        => new(result.VideoId, result.EnqueuedAt);
    
    public static GetAnalysisStatusQuery ToQuery(this GetAnalysisStatusRequest request) =>
        new(request.VideoId.ToString());

    public static GetAnalysisStatusResponse ToDto(this GetAnalysisStatusResult result)
        => new(result.Status, result.LastUpdatedAt);
    
    public static GetAnalysisResultsQuery ToQuery(this GetAnalysisResultsRequest request) =>
        new(request.VideoId.ToString());

    public static GetAnalysisResultsResponse ToDto(this GetAnalysisResultsResult result)
        => new(result.VideoId, result.Status, result.CompletedAt, result.TotalQrCodes, result.QrCodes.ToDto());
    
    private static IReadOnlyCollection<QrCodeResultDto> ToDto(this IReadOnlyCollection<QrCodeResult> results)
        => results.Select(r => r.ToDto()).ToList();
    
    private static QrCodeResultDto ToDto(this QrCodeResult result)
        => new(result.Text, result.TimestampSeconds, result.FormattedTimestamp, result.DetectedAt);
}