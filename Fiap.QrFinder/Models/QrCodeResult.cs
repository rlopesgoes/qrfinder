namespace Fiap.QrFinder.Models;

public class QrCodeResult
{
    public string Content { get; set; } = string.Empty;
    public int FrameNumber { get; set; }
    public double TimeInSeconds { get; set; }
    public QrCodeLocation Location { get; set; } = new();

}