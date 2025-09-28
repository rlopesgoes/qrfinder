namespace Domain.Models;

public record QrCodes(IReadOnlyCollection<QrCode> Values);

public record QrCode(string Content, Timestamp TimeStamp)
{
    public string FormattedTimestamp => TimeStamp.ToFormattedString();
}