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
        var resumeInfo = await GetResumeInfoAsync(videoId, cancellationToken);
        
        if (resumeInfo.IsNewUpload)
            await NotifyUploadStartedAsync(videoId, totalBytes, reporter, cancellationToken);

        await SkipToResumePointAsync(source, resumeInfo.BytesToSkip, cancellationToken);
        
        var uploadState = new UploadState(resumeInfo.NextSequence, resumeInfo.BytesReceived);
        
        await ProcessChunksAsync(videoId, source, totalBytes, reporter, uploadState, cancellationToken);
        
        await FinalizeUploadAsync(videoId, totalBytes, reporter, uploadState, cancellationToken);
    }

    private async Task<ResumeInfo> GetResumeInfoAsync(string videoId, CancellationToken cancellationToken)
    {
        var lastDto = await progressNotifier.GetAsync(videoId, cancellationToken);
        var lastSeq = lastDto?.LastSeq ?? -1;
        
        return new ResumeInfo(
            IsNewUpload: lastSeq == -1,
            NextSequence: lastSeq + 1,
            BytesToSkip: (lastSeq + 1) * ChunkSize,
            BytesReceived: Math.Max(0, (lastSeq + 1) * ChunkSize)
        );
    }

    private async Task NotifyUploadStartedAsync(
        string videoId, 
        long totalBytes, 
        IUploadReporter reporter,
        CancellationToken cancellationToken)
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

    private static async Task SkipToResumePointAsync(
        Stream source, 
        long bytesToSkip, 
        CancellationToken cancellationToken)
    {
        if (bytesToSkip <= 0) 
            return;
        
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

    private async Task ProcessChunksAsync(
        string videoId,
        Stream source,
        long totalBytes,
        IUploadReporter reporter,
        UploadState state,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[ChunkSize];

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead <= 0) break;

            await PublishChunkAsync(videoId, buffer, bytesRead, state.CurrentSequence, cancellationToken);

            state.IncrementProgress(bytesRead);
            
            await reporter.OnProgressAsync(
                videoId, 
                state.CurrentSequence - 1, 
                state.TotalBytesReceived, 
                totalBytes, 
                cancellationToken);
        }
    }

    private async Task PublishChunkAsync(
        string videoId,
        byte[] buffer,
        int length,
        long sequence,
        CancellationToken cancellationToken)
    {
        await producer.ProduceAsync(
            "videos.raw-chunks",
            new Message<string, byte[]>
            {
                Key = videoId,
                Value = buffer.AsMemory(0, length).ToArray(),
                Headers = [new Header("seq", BitConverter.GetBytes(sequence))]
            },
            cancellationToken);
    }

    private async Task FinalizeUploadAsync(
        string videoId,
        long totalBytes,
        IUploadReporter reporter,
        UploadState state,
        CancellationToken cancellationToken)
    {
        var lastSeq = state.CurrentSequence - 1;

        await producer.ProduceAsync(
            "videos.control",
            new Message<string, byte[]>
            {
                Key = videoId,
                Headers =
                [
                    new Header("type", "completed"u8.ToArray()),
                    new Header("lastSeq", BitConverter.GetBytes(lastSeq))
                ]
            },
            cancellationToken);

        await reporter.OnCompletedAsync(
            videoId, 
            lastSeq, 
            state.TotalBytesReceived, 
            totalBytes, 
            cancellationToken);
    }

    private record ResumeInfo(
        bool IsNewUpload,
        long NextSequence,
        long BytesToSkip,
        long BytesReceived);

    private class UploadState(long initialSequence, long initialBytes)
    {
        public long CurrentSequence { get; private set; } = initialSequence;
        public long TotalBytesReceived { get; private set; } = initialBytes;

        public void IncrementProgress(int bytesRead)
        {
            TotalBytesReceived += bytesRead;
            CurrentSequence++;
        }
    }
}