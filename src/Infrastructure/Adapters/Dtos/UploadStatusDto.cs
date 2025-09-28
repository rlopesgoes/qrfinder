using Domain.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Infrastructure.Adapters.Dtos;

internal sealed record UploadStatusDto
{
    [BsonId] public string VideoId { get; set; } = string.Empty;
    public Stage Stage { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}