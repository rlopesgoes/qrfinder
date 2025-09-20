namespace Fiap.QrFinder.Models;

public class VideoUploadRequest
{
    public IFormFile VideoFile { get; set; } = null!;
    public int? FrameInterval { get; set; }
}