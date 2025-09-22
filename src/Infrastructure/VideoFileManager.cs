using Application.Videos.Ports;

namespace Infrastructure;

public class VideoFileManager : IVideoFileManager
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "qrfinder", "videos");

    public string FinalizeVideo(string videoId)
    {
        var partPath = GetPartVideoPath(videoId);
        var finalPath = GetCompleteVideoPath(videoId);

        if (!File.Exists(partPath))
            return finalPath;

        if (File.Exists(finalPath))
            File.Delete(finalPath);

        File.Move(partPath, finalPath);
        return finalPath;
    }

    public void CleanupVideo(string videoPath)
    {
        if (File.Exists(videoPath))
            File.Delete(videoPath);
    }

    private string GetPartVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin.part");
    private string GetCompleteVideoPath(string videoId) => Path.Combine(_tempDirectory, $"{SanitizeFileName(videoId)}.bin");

    private static string SanitizeFileName(string fileName)
    {
        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        return result.Trim();
    }
}