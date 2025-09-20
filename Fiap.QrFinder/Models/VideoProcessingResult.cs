namespace Fiap.QrFinder.Models;

public class VideoProcessingResult
{
    public string VideoId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalFrames { get; set; }
    public double DurationInSeconds { get; set; }
    public List<QrCodeResult> QrCodes { get; set; } = new();

}