using Domain.Common;
using Domain.Models;
using FluentAssertions;
using Xunit;

namespace UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_Should_Create_Successful_Result()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.StatusCode.Should().Be(StatusCode.Success);
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void WithError_Should_Create_Error_Result()
    {
        // Arrange
        var errorMessage = "Test error";

        // Act
        var result = Result.WithError(errorMessage);

        // Assert
        result.StatusCode.Should().Be(StatusCode.Error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public void Generic_Success_Should_Create_Successful_Result_With_Value()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.StatusCode.Should().Be(StatusCode.Success);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void EntityNotFound_Should_Create_NotFound_Result()
    {
        // Arrange
        var message = "Entity not found";

        // Act
        var result = Result<string>.EntityNotFound(message);

        // Assert
        result.StatusCode.Should().Be(StatusCode.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(message);
    }

    [Fact]
    public void NoContent_Should_Create_NoContent_Result()
    {
        // Act
        var result = Result<string>.NoContent();

        // Assert
        result.StatusCode.Should().Be(StatusCode.NoContent);
        result.IsSuccessOrNoContent.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeNull();
    }
}

public class QrCodeTests
{
    [Fact]
    public void QrCode_Should_Have_Content_And_Timestamp()
    {
        // Arrange
        var content = "https://example.com";
        var timestamp = new Timestamp(30.0);

        // Act
        var qrCode = new QrCode(content, timestamp);

        // Assert
        qrCode.Content.Should().Be(content);
        qrCode.TimeStamp.Should().Be(timestamp);
        qrCode.FormattedTimestamp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void QrCodes_Should_Accept_Collection_Of_QrCodes()
    {
        // Arrange
        var qrCodeList = new List<QrCode>
        {
            new("https://example1.com", new Timestamp(10.0)),
            new("https://example2.com", new Timestamp(20.0))
        };

        // Act
        var qrCodes = new QrCodes(qrCodeList);

        // Assert
        qrCodes.Values.Should().HaveCount(2);
        qrCodes.Values.Should().BeEquivalentTo(qrCodeList);
    }
}