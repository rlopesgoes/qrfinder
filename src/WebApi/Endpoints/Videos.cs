using Application.Videos.VideoUploader;
using Application.Videos.GetVideoStatus;
using Application.Videos.GetVideoResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Endpoints;

public static class Videos
{
    private const string QrCodeFinder = "QR Code finder";

    public static void MapVideosEndpoints(this WebApplication app)
    {
        app.MapPost("/video/upload",
                async (
                    [FromHeader(Name = "X-Video-Id")] Guid videoId,
                    [FromForm(Name = "file")] IFormFile file,
                    IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (file.Length == 0)
                        return Results.BadRequest("Invalid File");

                    var totalBytes = file.Length;
                    await using var stream = file.OpenReadStream();

                    await mediator.Send(new VideoUploaderRequest(videoId.ToString("N"), totalBytes, stream), cancellationToken);

                    return Results.Accepted($"/video/{videoId:D}/status", new { videoId = videoId.ToString("D") });
                })
            .DisableAntiforgery()
            .WithName("Upload video for qr code finding")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
        
        app.MapGet("/video/{id:guid}/status", async (IMediator mediator, Guid id) =>
            {
                var result = await mediator.Send(new GetVideoStatusRequest(id.ToString("N")));
                
                return result is null ? 
                    Results.NotFound(new { message = "Video not found or not processed yet" }) :
                    Results.Ok(result);
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