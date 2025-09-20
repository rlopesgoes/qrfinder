using Fiap.QrFinder.Models;
using Fiap.QrFinder.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger to handle file uploads
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "QR Code Finder API", Version = "v1" });

    // Add support for file uploads in Swagger UI
    options.OperationFilter<SwaggerFileOperationFilter>();
});

// Register our video processing service
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

// Add health checks
builder.Services.AddHealthChecks();

// Configure file size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

// Configure form options for large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Add health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

// API endpoint to upload and process a video
app.MapPost("/api/videos/upload", async ([FromForm] VideoUploadRequest request, IVideoProcessingService videoService) =>
{
    try
    {
        if (request.VideoFile == null || request.VideoFile.Length == 0)
            return Results.BadRequest("No video file uploaded");

        if (!request.VideoFile.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Only MP4 videos are supported");

        int frameInterval = request.FrameInterval ?? 1;

        var filePath = await videoService.SaveUploadedFileAsync(request.VideoFile);
        var result = await videoService.ProcessVideoAsync(filePath, frameInterval);

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing video: {ex.Message}");
    }
})
.WithName("UploadVideo")
.WithOpenApi()
.DisableAntiforgery();

app.Run();

// Swagger operation filter to handle file uploads
public class SwaggerFileOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType.IsAssignableTo(typeof(IFormFile)));

        if (fileParameters.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = context.SchemaRepository.Schemas
                                .Where(s => s.Key == nameof(VideoUploadRequest))
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.Value
                                )
                                .FirstOrDefault().Value?.Properties ?? new Dictionary<string, OpenApiSchema>(),
                            Required = new HashSet<string> { "VideoFile" }
                        }
                    }
                }
            };
        }
    }
}