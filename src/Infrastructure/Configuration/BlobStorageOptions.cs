namespace Infrastructure.Configuration;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    
    public string ConnectionString { get; set; } = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;";
    public string ContainerName { get; set; } = "videos";
    public int MinutesToExpire { get; set; } = 1;
}