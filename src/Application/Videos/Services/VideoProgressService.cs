using Application.Videos.Ports;
using Application.Videos.Ports.Dtos;
using Domain.Videos;

namespace Application.Videos.Services;

/// <summary>
/// Service for video processing progress (upload + QR detection)
/// Abstracts technical details but saves to existing MongoDB structure
/// </summary>
public class VideoProgressService(
    IVideoProgressNotifier progressNotifier,
    IVideoStatusRepository videoStatusRepository)
{
    public async Task UpdateStatusAsync(string videoId, VideoProcessingStage stage, double progressPercentage = 0.0, string? currentOperation = null, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        // Update MongoDB (reuse existing UploadStatus structure for entire process)
        var status = new UploadStatus(videoId, stage, null, null, null, DateTime.UtcNow);
        await videoStatusRepository.UpsertAsync(status, cancellationToken);
        
        // Notify SignalR
        await progressNotifier.NotifyProgressAsync(videoId, stage.ToString(), progressPercentage, errorMessage, cancellationToken);
    }

    public async Task<UploadStatus?> GetStatusAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await videoStatusRepository.GetAsync(videoId, cancellationToken);
    }

    // Helper methods for common operations
    // Total process: Upload (0-50%) + Processing (50-100%)
    
    public async Task StartUploadingAsync(string videoId, CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(videoId, VideoProcessingStage.Uploading, 0.0, "Starting upload", null, cancellationToken);

    public async Task CompleteUploadAsync(string videoId, CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(videoId, VideoProcessingStage.Uploaded, 50.0, "Upload completed", null, cancellationToken);

    public async Task StartProcessingAsync(string videoId, CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(videoId, VideoProcessingStage.Processing, 50.0, "Starting QR code detection", null, cancellationToken);

    public async Task UpdateProcessingProgressAsync(string videoId, double processingProgress, string? operation = null, CancellationToken cancellationToken = default)
    {
        // Processing progress from 0-100% maps to 50-100% of total
        var totalProgress = 50.0 + (processingProgress * 0.5);
        await UpdateStatusAsync(videoId, VideoProcessingStage.Processing, totalProgress, operation ?? "Processing QR codes", null, cancellationToken);
    }

    public async Task CompleteProcessingAsync(string videoId, int qrCodesFound, CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(videoId, VideoProcessingStage.Processed, 100.0, $"Completed - {qrCodesFound} QR codes found", null, cancellationToken);

    public async Task FailAsync(string videoId, string errorMessage, CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(videoId, VideoProcessingStage.Failed, 0.0, "Processing failed", errorMessage, cancellationToken);

    private static double CalculateProgressPercentage(VideoProcessingStage stage) => stage switch
    {
        VideoProcessingStage.Uploading => 25.0,
        VideoProcessingStage.Uploaded => 50.0,
        VideoProcessingStage.Processing => 75.0,
        VideoProcessingStage.Processed => 100.0,
        VideoProcessingStage.Failed => 0.0,
        _ => 0.0
    };

    private static string GetCurrentOperation(VideoProcessingStage stage) => stage switch
    {
        VideoProcessingStage.Uploading => "Uploading video",
        VideoProcessingStage.Uploaded => "Upload completed",
        VideoProcessingStage.Processing => "Processing QR codes",
        VideoProcessingStage.Processed => "Processing completed",
        VideoProcessingStage.Failed => "Processing failed",
        _ => "Unknown"
    };
}