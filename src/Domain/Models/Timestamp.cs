namespace Domain.Models;

public record Timestamp(double Seconds)
{ 
    public string ToFormattedString() => TimeSpan.FromSeconds(Seconds).ToString(@"mm\:ss\.fff");
    public DateTime ToDateTime(DateTime baseTime) => baseTime.AddSeconds(Seconds);
}