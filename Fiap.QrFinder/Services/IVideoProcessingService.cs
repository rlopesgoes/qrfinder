using Fiap.QrFinder.Models;

namespace Fiap.QrFinder.Services;

public interface IVideoProcessingService
{
    Task<string> SaveUploadedFileAsync(IFormFile file);
    Task<VideoProcessingResult> ProcessVideoAsync(string filePath, int frameInterval = 1);
}