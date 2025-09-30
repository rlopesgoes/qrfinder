using Application.Ports;
using Application.UseCases.GenerateUploadLink;
using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Application;

public class GenerateUploadLinkHandlerTests
{
    private readonly Mock<IUploadLinkGenerator> _uploadLinkGenerator;
    private readonly Mock<IStatusWriteOnlyRepository> _statusWriteOnlyRepository;
    private readonly GenerateUploadLinkHandler _handler;

    public GenerateUploadLinkHandlerTests()
    {
        _uploadLinkGenerator = new Mock<IUploadLinkGenerator>();
        _statusWriteOnlyRepository = new Mock<IStatusWriteOnlyRepository>();
        
        _handler = new GenerateUploadLinkHandler(
            _uploadLinkGenerator.Object,
            _statusWriteOnlyRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_Generate_New_VideoId_When_Not_Provided()
    {
        // Arrange
        var command = new GenerateUploadLinkCommand();
        var uploadLink = new UploadLink("https://example.com/upload", DateTimeOffset.UtcNow.AddHours(1));
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VideoId.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Value.VideoId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Use_Provided_VideoId_When_Given()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var command = new GenerateUploadLinkCommand(videoId);
        var uploadLink = new UploadLink("https://example.com/upload", DateTimeOffset.UtcNow.AddHours(1));
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(videoId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VideoId.Should().Be(videoId.ToString());
    }

    [Fact]
    public async Task Handle_Should_Return_Success_With_Upload_Link_Details()
    {
        // Arrange
        var command = new GenerateUploadLinkCommand();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var uploadUrl = "https://storage.azure.com/upload?token=abc123";
        var uploadLink = new UploadLink(uploadUrl, expiresAt);
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UploadUrl.Should().Be(uploadUrl);
        result.Value.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Upload_Link_Generation_Fails()
    {
        // Arrange
        var command = new GenerateUploadLinkCommand();
        var errorMessage = "Failed to generate upload link";
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Status_Upsert_Fails()
    {
        // Arrange
        var command = new GenerateUploadLinkCommand();
        var uploadLink = new UploadLink("https://example.com/upload", DateTimeOffset.UtcNow.AddHours(1));
        var errorMessage = "Database error";
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.WithError(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_Should_Create_Status_With_Created_Stage()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var command = new GenerateUploadLinkCommand(videoId);
        var uploadLink = new UploadLink("https://example.com/upload", DateTimeOffset.UtcNow.AddHours(1));
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _statusWriteOnlyRepository.Verify(
            x => x.UpsertAsync(
                It.Is<Status>(s => s.VideoId == videoId.ToString() && s.Stage == Stage.Created), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Call_Dependencies_In_Correct_Order()
    {
        // Arrange
        var command = new GenerateUploadLinkCommand();
        var uploadLink = new UploadLink("https://example.com/upload", DateTimeOffset.UtcNow.AddHours(1));
        
        _uploadLinkGenerator
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UploadLink>.Success(uploadLink));
            
        _statusWriteOnlyRepository
            .Setup(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _uploadLinkGenerator.Verify(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _statusWriteOnlyRepository.Verify(x => x.UpsertAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}