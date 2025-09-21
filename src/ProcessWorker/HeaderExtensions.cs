using Confluent.Kafka;

namespace Worker;

public static class HeaderExtensions
{
    public static string? GetUtf8(this Headers headers, string key)
    {
        var header = headers.LastOrDefault(x => x.Key == key);
        
        return header is null ? null :
            System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
    }

    public static long? GetInt64(this Headers headers, string key)
    {
        var header = headers.LastOrDefault(x => x.Key == key);
        
        if (header is null) 
            return null;
        
        var bytes = header.GetValueBytes();
        
        return bytes.Length switch
        {
            8 => BitConverter.ToInt64(bytes),
            4 => BitConverter.ToInt32(bytes),
            _ => null
        };
    }
}