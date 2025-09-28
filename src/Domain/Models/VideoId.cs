namespace Domain.Models;

public record VideoId(Guid Value)
{
    public static VideoId New() => new(Guid.NewGuid());
    public static VideoId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}