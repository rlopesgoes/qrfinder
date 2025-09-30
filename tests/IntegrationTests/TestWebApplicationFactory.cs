using Application.Ports;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntegrationTests;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017/test",
                ["MongoDb:DatabaseName"] = "qrfinder_test",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:VideoAnalysisQueue"] = "video-analysis-queue-test",
                ["Kafka:AnalysisResults"] = "analysis-results-test",
                ["Kafka:Notifications"] = "notifications-test",
                ["Kafka:ProgressUpdates"] = "progress-updates-test",
                ["BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["BlobStorage:ContainerName"] = "videos-test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real dependencies with mocks for testing
            var statusReadOnlyMock = new Mock<IStatusReadOnlyRepository>();
            var statusWriteOnlyMock = new Mock<IStatusWriteOnlyRepository>();
            var uploadLinkGeneratorMock = new Mock<IUploadLinkGenerator>();
            var videoAnalysisQueueMock = new Mock<IVideoAnalysisQueue>();
            var progressNotifierMock = new Mock<IProgressNotifier>();
            var analysisResultRepositoryMock = new Mock<IAnalysisResultReadOnlyRepository>();

            // Configure mocks with default behaviors
            statusReadOnlyMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string videoId, CancellationToken _) => 
                    Result<Status>.Success(new Status(videoId, Stage.Created, DateTime.UtcNow)));

            statusWriteOnlyMock.Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            uploadLinkGeneratorMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<UploadLink>.Success(new UploadLink("https://test-upload-url.com", DateTimeOffset.UtcNow.AddHours(1))));

            videoAnalysisQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            progressNotifierMock.Setup(x => x.NotifyAsync(It.IsAny<ProgressNotification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            analysisResultRepositoryMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AnalysisResult>.EntityNotFound("No results found"));

            // Remove existing registrations and add mocks
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStatusReadOnlyRepository));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(statusReadOnlyMock.Object);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStatusWriteOnlyRepository));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(statusWriteOnlyMock.Object);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUploadLinkGenerator));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(uploadLinkGeneratorMock.Object);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVideoAnalysisQueue));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(videoAnalysisQueueMock.Object);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProgressNotifier));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(progressNotifierMock.Object);

            descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAnalysisResultReadOnlyRepository));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(analysisResultRepositoryMock.Object);

            services.AddLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }
}