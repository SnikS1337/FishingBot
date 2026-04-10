using FishingBot.Core.Contracts;
using Xunit;

namespace FishingBot.Tests.V2.Contracts;

public class InputBindingTests
{
    [Fact]
    public void Constructor_AllowsArbitraryControlToken()
    {
        // Arrange

        // Act
        var binding = new InputBinding("MouseLeft");

        // Assert
        Assert.Equal("MouseLeft", binding.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenTokenIsBlank()
    {
        // Arrange

        // Act
        var action = () => new InputBinding(" ");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void EqualTokens_CompareEqual()
    {
        // Arrange
        var left = new InputBinding("MouseLeft");
        var right = new InputBinding("MouseLeft");

        // Act / Assert
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
