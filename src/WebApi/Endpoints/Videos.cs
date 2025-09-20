using Application.Queries;
using MediatR;

namespace WebApi.Endpoints;

public static class Videos
{
    public static void MapVideosEndpoints(this WebApplication app)
    {
        app.MapGet("/test", async (IMediator mediator, string? message) =>
            {
                var query = new TestQuery(message ?? "Hello from MediatR!");
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("TestMediatR")
            .WithOpenApi();
    }
}