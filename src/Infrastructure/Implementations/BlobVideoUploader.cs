// using System.Diagnostics;
// using Application.Videos.Ports;
// using Confluent.Kafka;
// using Infrastructure.Telemetry;
// using Microsoft.Extensions.Logging;
//
// namespace Infrastructure.Implementations;
//
// public class BlobVideoUploader(
//     IBlobStorageService blobStorage,
//     IProcessStatusRepository progressNotifier,
//     IProducer<string, byte[]> producer,
//     ILogger<BlobVideoUploader> logger,
//     ActivitySource activitySource) : IVideoUploader
// {
//     private const int ChunkSize = 512 * 1024;
//     private const int SkipBufferSize = 64 * 1024;
//     
//     public async Task UploadAsync(
//         string videoId, 
//         long totalBytes, 
//         Stream source, 
//         IUploadReporter reporter,
//         CancellationToken cancellationToken)
//     {
//         using var activity = activitySource.StartActivity("VideoUpload");
//         activity?.SetTag("video.id", videoId);
//         activity?.SetTag("video.totalBytes", totalBytes);
//         
//         logger.LogInformation("Starting video upload for {VideoId} with {TotalBytes} bytes", videoId, totalBytes);
//         
//         var resumeInfo = await GetResumeInfoAsync(videoId, cancellationToken);
//         
//         if (resumeInfo.IsNewUpload)
//         {
//             await NotifyUploadStartedAsync(videoId, totalBytes, reporter, cancellationToken);
//         }
//         else
//         {
//             logger.LogInformation("Resuming upload for {VideoId} from sequence {NextSequence}", videoId, resumeInfo.NextSequence);
//         }
//
//         await SkipToResumePointAsync(source, resumeInfo.BytesToSkip, cancellationToken);
//         
//         var uploadState = new UploadState(resumeInfo.NextSequence, resumeInfo.BytesReceived);
//         
//         await ProcessChunksAsync(videoId, source, totalBytes, reporter, uploadState, cancellationToken);
//         
//         await FinalizeUploadAsync(videoId, totalBytes, reporter, uploadState, cancellationToken);
//         
//         logger.LogInformation("Video upload completed for {VideoId}. Total chunks: {TotalChunks}", videoId, uploadState.CurrentSequence);
//         activity?.SetStatus(ActivityStatusCode.Ok);
//     }
//
//     private async Task<ResumeInfo> GetResumeInfoAsync(string videoId, CancellationToken cancellationToken)
//     {
//         var lastDto = await progressNotifier.GetAsync(videoId, cancellationToken);
//         var lastSeq = lastDto?.LastSeq ?? -1;
//         
//         return new ResumeInfo(
//             IsNewUpload: lastSeq == -1,
//             NextSequence: lastSeq + 1,
//             BytesToSkip: (lastSeq + 1) * ChunkSize,
//             BytesReceived: Math.Max(0, (lastSeq + 1) * ChunkSize)
//         );
//     }
//
//     private async Task NotifyUploadStartedAsync(
//         string videoId, 
//         long totalBytes, 
//         IUploadReporter reporter,
//         CancellationToken cancellationToken)
//     {
//         using var activity = activitySource.StartActivity("NotifyUploadStarted");
//         activity?.SetTag("video.id", videoId);
//         
//         await reporter.OnStartedAsync(videoId, totalBytes, cancellationToken);
//         
//         // Only send control message to Kafka, not chunks
//         var headers = new Headers
//         {
//             new Header("type", "started"u8.ToArray())
//         };
//         
//         // Inject trace context into Kafka headers
//         KafkaTraceContextPropagator.InjectTraceContext(headers);
//         
//         await producer.ProduceAsync(
//             "videos.control",
//             new Message<string, byte[]>
//             {
//                 Key = videoId,
//                 Headers = headers
//             }, 
//             cancellationToken);
//             
//         logger.LogInformation("Upload started notification sent for {VideoId}", videoId);
//     }
//
//     private static async Task SkipToResumePointAsync(
//         Stream source, 
//         long bytesToSkip, 
//         CancellationToken cancellationToken)
//     {
//         if (bytesToSkip <= 0) 
//             return;
//         
//         var skipBuffer = new byte[Math.Min(SkipBufferSize, ChunkSize)];
//         var remaining = bytesToSkip;
//
//         while (remaining > 0)
//         {
//             var toRead = (int)Math.Min(remaining, skipBuffer.Length);
//             var bytesRead = await source.ReadAsync(skipBuffer.AsMemory(0, toRead), cancellationToken);
//             
//             if (bytesRead <= 0) break;
//             remaining -= bytesRead;
//         }
//     }
//
//     private async Task ProcessChunksAsync(
//         string videoId,
//         Stream source,
//         long totalBytes,
//         IUploadReporter reporter,
//         UploadState state,
//         CancellationToken cancellationToken)
//     {
//         var buffer = new byte[ChunkSize];
//
//         while (true)
//         {
//             var bytesRead = await source.ReadAsync(buffer, cancellationToken);
//             if (bytesRead <= 0) break;
//
//             // Upload chunk to Azurite instead of Kafka
//             var chunkData = buffer.AsMemory(0, bytesRead).ToArray();
//             await blobStorage.UploadChunkAsync(videoId, (int)state.CurrentSequence, chunkData, cancellationToken);
//
//             state.IncrementProgress(bytesRead);
//             
//             await reporter.OnProgressAsync(
//                 videoId, 
//                 state.CurrentSequence - 1, 
//                 state.TotalBytesReceived, 
//                 totalBytes, 
//                 cancellationToken);
//         }
//     }
//
//     private async Task FinalizeUploadAsync(
//         string videoId,
//         long totalBytes,
//         IUploadReporter reporter,
//         UploadState state,
//         CancellationToken cancellationToken)
//     {
//         using var activity = activitySource.StartActivity("FinalizeUpload");
//         activity?.SetTag("video.id", videoId);
//         
//         var lastSeq = state.CurrentSequence - 1;
//
//         // Send completion message to Kafka for processing trigger
//         var headers = new Headers
//         {
//             new Header("type", "completed"u8.ToArray()),
//             new Header("lastSeq", BitConverter.GetBytes(lastSeq))
//         };
//         
//         // Inject trace context into Kafka headers
//         KafkaTraceContextPropagator.InjectTraceContext(headers);
//         
//         await producer.ProduceAsync(
//             "videos.control",
//             new Message<string, byte[]>
//             {
//                 Key = videoId,
//                 Headers = headers
//             },
//             cancellationToken);
//
//         await reporter.OnCompletedAsync(
//             videoId, 
//             lastSeq, 
//             state.TotalBytesReceived, 
//             totalBytes, 
//             cancellationToken);
//             
//         logger.LogInformation("Upload finalized for {VideoId}. LastSeq: {LastSeq}, TotalBytes: {TotalBytes}", 
//             videoId, lastSeq, state.TotalBytesReceived);
//     }
//
//     private record ResumeInfo(
//         bool IsNewUpload,
//         long NextSequence,
//         long BytesToSkip,
//         long BytesReceived);
//
//     private class UploadState(long initialSequence, long initialBytes)
//     {
//         public long CurrentSequence { get; private set; } = initialSequence;
//         public long TotalBytesReceived { get; private set; } = initialBytes;
//
//         public void IncrementProgress(int bytesRead)
//         {
//             TotalBytesReceived += bytesRead;
//             CurrentSequence++;
//         }
//     }
// }