using Application.Videos.Common;
using Application.Videos.Data;
using Confluent.Kafka;

namespace Infrastructure.Implementations;

public class KafkaVideoUploader(
    IProducer<string, byte[]> producer,
    IVideoStatusRepository progressNotifier) : IVideoUploader
{
    private const int ChunkSize = 512 * 1024;
    private const int SkipBufferSize = 64 * 1024;
    
    public async Task UploadAsync(
        string videoId, 
        long totalBytes, 
        Stream source, 
        IUploadReporter reporter,
        CancellationToken cancellationToken)
    {
        var lastDto = await progressNotifier.GetLastSeqAsync(videoId, cancellationToken);
        var lastSeq = lastDto?.LastSeq ?? -1;

        if (lastSeq == -1)
        {
            await reporter.OnStartedAsync(videoId, totalBytes, cancellationToken);
            await producer.ProduceAsync(
                "videos.control",
                new Message<string, byte[]>
                {
                    Key = videoId,
                    Headers = [new Header("type", "started"u8.ToArray())]
                }, 
                cancellationToken);
        }

        var bytesToSkip = (long)(lastSeq + 1) * ChunkSize;
        await SkipBytesAsync(source, bytesToSkip, cancellationToken);

        var buffer = new byte[ChunkSize];
        var seq = lastSeq + 1;
        var totalReceived = Math.Max(0, (long)(lastSeq + 1) * ChunkSize);

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead <= 0) break;

            await producer.ProduceAsync(
                "videos.raw-chunks",
                new Message<string, byte[]>
                {
                    Key = videoId,
                    Value = buffer.AsMemory(0, bytesRead).ToArray(),
                    Headers = [new Header("seq", BitConverter.GetBytes(seq))]
                },
                cancellationToken);

            totalReceived += bytesRead;
            await reporter.OnProgressAsync(videoId, seq, totalReceived, totalBytes, cancellationToken);
            seq++;
        }

        var finalSeq = seq - 1;

        await producer.ProduceAsync(
            "videos.control",
            new Message<string, byte[]>
            {
                Key = videoId,
                Headers =
                [
                    new Header("type", "completed"u8.ToArray()),
                    new Header("lastSeq", BitConverter.GetBytes(finalSeq))
                ]
            },
            cancellationToken);

        await reporter.OnCompletedAsync(videoId, finalSeq, totalReceived, totalBytes, cancellationToken);
    }

    private static async Task SkipBytesAsync(Stream source, long bytesToSkip, CancellationToken cancellationToken)
    {
        if (bytesToSkip <= 0) return;
        
        var skipBuffer = new byte[Math.Min(SkipBufferSize, ChunkSize)];
        var remaining = bytesToSkip;

        while (remaining > 0)
        {
            var toRead = (int)Math.Min(remaining, skipBuffer.Length);
            var bytesRead = await source.ReadAsync(skipBuffer.AsMemory(0, toRead), cancellationToken);
            
            if (bytesRead <= 0) break;
            remaining -= bytesRead;
        }
    }
}