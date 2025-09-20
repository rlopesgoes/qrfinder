using System.Net;
using Application.Queries;
using MediatR;

namespace WebApi.Endpoints;

public static class Videos
{
    private const string QrCodeFinder = "QR Code finder";

    public static void MapVideosEndpoints(this WebApplication app)
    {
        app.MapPost("/video/upload", async (IMediator mediator, string? message) =>
            {
                return Results.Ok(true);
            })
            .WithName("Upload video for qr code finding")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
        
        app.MapGet("/video/{id:guid}/status", async (IMediator mediator, Guid id, string? message) =>
            {
                var query = new TestQuery(message ?? "Hello from MediatR!");
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("Get status of video processing")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
        
        app.MapGet("/video/{id:guid}/results", async (IMediator mediator, Guid id, string? message) =>
            {
                var query = new TestQuery(message ?? "Hello from MediatR!");
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("Get results of qr codes finding")
            .WithTags(QrCodeFinder)
            .WithOpenApi();
    }
}