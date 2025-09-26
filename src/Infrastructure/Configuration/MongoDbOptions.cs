namespace Infrastructure.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDB";
    
    public string ConnectionString { get; set; } = "mongodb://admin:password123@localhost:27017/qrfinder?authSource=admin";
    public string Database { get; set; } = "qrfinder";
}