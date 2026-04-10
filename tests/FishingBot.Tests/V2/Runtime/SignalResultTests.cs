using FishingBot.Core.Contracts;
using Xunit;

namespace FishingBot.Tests.V2.Runtime;

public class SignalResultTests
{
    [Fact]
    public void Detected_ReturnsDetectedSignalWithConfidenceAndData()
    {
        // Arrange
        var data = new TestSignalData(42);

        // Act
        var result = SignalResult<TestSignalData>.Detected(0.85, data);

        // Assert
        Assert.True(result.IsDetected);
        Assert.Equal(0.85, result.Confidence);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public void NotDetected_ReturnsNonDetectedSignalWithZeroConfidence()
    {
        // Arrange

        // Act
        var result = SignalResult<TestSignalData>.NotDetected();

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Detected_Throws_WhenConfidenceIsOutsideNormalizedRange(double confidence)
    {
        // Arrange
        var data = new TestSignalData(42);

        // Act
        var action = () => SignalResult<TestSignalData>.Detected(confidence, data);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Detected_Throws_WhenConfidenceIsNaN()
    {
        // Arrange
        var data = new TestSignalData(42);

        // Act
        var action = () => SignalResult<TestSignalData>.Detected(double.NaN, data);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Detected_Throws_WhenDataIsNull()
    {
        // Arrange

        // Act
        var action = () => SignalResult<TestSignalData>.Detected(0.75, null!);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    private sealed record TestSignalData(int MarkerX);
}
