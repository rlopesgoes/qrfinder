using Application.Videos.Features.GetVideoResults;
using Application.Videos.Features.GetVideoStatus;
using Application.Videos.Features.VideoUploader;
using Application.UseCases;
using Application.UseCases.GetAnalysisStatus;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Endpoints;

public record GenerateUploadLinkRequest(Guid? VideoId = null);
public record GenerateUploadLinkResponse(string VideoId, string Url, DateTimeOffset ExpiresAt);
public record AnalyzeVideoRequest(Guid VideoId);

public static class Videos
{
    private const string QrCodeFinder = "QR Code finder";

    public static void MapVideosEndpoints(this WebApplication app)
    {
        app.MapPost("/video/upload-link/generate", async
                (GenerateUploadLinkRequest? request, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new GenerateUploadLinkCommand(request?.VideoId);

                var result = await mediator.Send(command, cancellationToken);

                if (!result.IsSuccess)
                    return Results.Problem(
                        title: "Error on generating upload link",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);

                return Results.Ok(new GenerateUploadLinkResponse(result.Value!.VideoId, result.Value!.UploadUrl,
                    result.Value!.ExpiresAt));
            })
            .WithName("Generate upload link")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapPatch("/video/{id:guid}/analyze", async
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new EnqueueVideoForAnalyzingCommand(id.ToString()), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Problem(
                        title: $"Failed to enqueue video {id}",
                        detail: result.Error?.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
            })
            .WithName("Analyze video")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapGet("/video/{id:guid}/status",
                async (IMediator mediator, Guid id, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetAnalysisStatusQuery(id.ToString()), cancellationToken);

                    if (!result.IsSuccessOrNoContent)
                        Results.Problem(
                            title: $"Failed to get video video {id}",
                            detail: result.Error?.Message,
                            statusCode: StatusCodes.Status500InternalServerError);

                    if (result.StatusCode is StatusCode.NoContent)
                        return Results.NoContent();

                    return Results.Ok(result.Value!);
                })
            .WithName("Get status of video processing")
            .WithTags(QrCodeFinder)
            .WithOpenApi();

        app.MapGet("/video/{id:guid}/results", async (IMediator mediator, Guid id) =>
            {
                var result = await mediator.Send(new GetVideoResultsRequest(id.ToString("N")));

                if (result == null)
                {
                    return Results.NotFound(new { message = "Video not found or not processed yet" });
                }

                return Results.Ok(result);
            })
            .WithName("Get results of qr codes finding")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
    }
}