using SmartHome.Domain.Device;

namespace SmartHome.Domain.Tests.Device;

public class CommandHistoryTests
{
    [Fact]
    public void Constructor_ValidArguments_SetsPropertiesAndStampsTimestamp()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        const string operation = "Turned On";

        // Act
        var history = new CommandHistory(deviceId, operation);

        // Assert
        Assert.Equal(deviceId, history.DeviceId);
        Assert.Equal(operation, history.Operation);
        // Timestamp should be set to "now" — generous tolerance to avoid flaky CI.
        Assert.True((DateTime.UtcNow - history.Timestamp).TotalSeconds < 5);
    }
}
