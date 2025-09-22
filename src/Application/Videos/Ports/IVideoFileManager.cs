namespace Application.Videos.Ports;

public interface IVideoFileManager
{
    string FinalizeVideo(string videoId);
    void CleanupVideo(string videoPath);
}