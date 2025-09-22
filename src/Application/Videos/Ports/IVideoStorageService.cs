namespace Application.Videos.Ports;

public interface IVideoStorageService
{
    Task StoreVideoPartAsync(string videoId, byte[] videoPart, CancellationToken cancellationToken);
}