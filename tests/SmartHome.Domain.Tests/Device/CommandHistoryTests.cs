using SmartHome.Domain.Device;

namespace SmartHome.Domain.Tests.Device;

public class CommandHistoryTests
{
    [Fact]
    public void Constructor_ValidArguments_SetsPropertiesAndStampsTimestamp()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        const string deviceName = "Kitchen Fan";
        const string deviceLocation = "Kitchen";
        const string deviceType = "Fan";
        const string operation = "Turned On";

        // Act
        var history = new CommandHistory(
            deviceId,
            deviceName,
            deviceLocation,
            deviceType,
            operation);

        // Assert
        Assert.Equal(deviceId, history.DeviceId);
        Assert.Equal(deviceName, history.DeviceName);
        Assert.Equal(deviceLocation, history.DeviceLocation);
        Assert.Equal(deviceType, history.DeviceType);
        Assert.Equal(operation, history.Operation);

        // Timestamp should be set to "now" — generous tolerance to avoid flaky CI.
        Assert.True((DateTime.UtcNow - history.Timestamp).TotalSeconds < 5);
    }
}