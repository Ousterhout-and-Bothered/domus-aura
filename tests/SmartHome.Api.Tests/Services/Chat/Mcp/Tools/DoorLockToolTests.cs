using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class DoorLockToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly DoorLockTool _tool;

    public DoorLockToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new DoorLockTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task LockDoorAsync_ReturnsErrorMessage_WhenNameIsMissing()
    {
        // Act
        var result = await _tool.LockDoorAsync(string.Empty);

        // Assert
        Assert.Equal("I need a door name to control a door lock.", result);
    }

    [Fact]
    public async Task LockDoorAsync_LocksDoor_WhenValidNameProvided()
    {
        // Arrange
        var name = "Front Door";
        var door = new DoorLock(Guid.NewGuid(), name, "Entrance");
        // Door is unlocked by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { door });

        // Act
        var result = await _tool.LockDoorAsync(name);

        // Assert
        Assert.Contains("Locked Front Door.", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(door.Id, "Lock", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockDoorAsync_UnlocksDoor_WhenValidNameProvided()
    {
        // Arrange
        var name = "Back Door";
        var door = new DoorLock(Guid.NewGuid(), name, "Kitchen");
        door.Lock(); // Make it locked

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { door });

        // Act
        var result = await _tool.UnlockDoorAsync(name);

        // Assert
        Assert.Contains("Unlocked Back Door.", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(door.Id, "Unlock", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
