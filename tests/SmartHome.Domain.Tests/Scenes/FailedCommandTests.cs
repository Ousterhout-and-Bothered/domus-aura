using SmartHome.Domain.Device.Commands;

namespace SmartHome.Domain.Tests.Scenes;

public class FailedCommandTests
{
    [Fact]
    public void Execute_ReturnsFailedResultWithGivenOperationAndMessage()
    {
        // Arrange
        var command = new FailedCommand("Lock", "Target device no longer registered.");

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal("Lock", result.Operation);
        Assert.False(result.Success);
        Assert.Equal("Target device no longer registered.", result.Message);
    }

    [Fact]
    public void OperationName_MatchesConstructorArgument()
    {
        // Arrange
        var command = new FailedCommand("SetPower", "ignored");

        // Act + Assert
        Assert.Equal("SetPower", command.OperationName);
    }
}

