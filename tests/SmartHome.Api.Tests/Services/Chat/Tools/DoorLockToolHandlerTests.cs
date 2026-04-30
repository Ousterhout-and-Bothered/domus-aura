using System.Text.Json;
using Moq;
using SmartHome.Api.Services.Chat.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Api.Tests.Services.Chat.Tools;

public class DoorLockToolHandlerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;

    public DoorLockToolHandlerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenNameIsMissing()
    {
        // Arrange
        var handler = new DoorLockToolHandler(_deviceServiceMock.Object, true);
        var arguments = new Dictionary<string, JsonElement>();

        // Act
        var result = await handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("I need a door name to control a door lock.", result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenDoorNotFound()
    {
        // Arrange
        var handler = new DoorLockToolHandler(_deviceServiceMock.Object, true);
        var doorName = "Front Door";
        var arguments = new Dictionary<string, JsonElement>
        {
            { "name", JsonDocument.Parse($"\"{doorName}\"").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device>());

        // Act
        var result = await handler.HandleAsync(arguments);

        // Assert
        Assert.Equal($"I could not find a door named {doorName}.", result);
    }

    [Fact]
    public async Task HandleAsync_LocksDoor_WhenValidArgumentsProvided()
    {
        // Arrange
        var handler = new DoorLockToolHandler(_deviceServiceMock.Object, true); // shouldLock = true
        var doorName = "Front Door";
        var door = new DoorLock(Guid.NewGuid(), doorName, "Entrance");
        // Default is Unlocked

        var arguments = new Dictionary<string, JsonElement>
        {
            { "name", JsonDocument.Parse($"\"{doorName}\"").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { door });

        // Act
        var result = await handler.HandleAsync(arguments);

        // Assert
        Assert.Contains($"Locked {doorName}", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(door.Id, "Lock", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnlocksDoor_WhenValidArgumentsProvided()
    {
        // Arrange
        var handler = new DoorLockToolHandler(_deviceServiceMock.Object, false); // shouldLock = false
        var doorName = "Front Door";
        var door = new DoorLock(Guid.NewGuid(), doorName, "Entrance");
        door.Lock(); // Start Locked

        var arguments = new Dictionary<string, JsonElement>
        {
            { "name", JsonDocument.Parse($"\"{doorName}\"").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { door });

        // Act
        var result = await handler.HandleAsync(arguments);

        // Assert
        Assert.Contains($"Unlocked {doorName}", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(door.Id, "Unlock", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReportsAlreadyCorrect_WhenStateIsSame()
    {
        // Arrange
        var handler = new DoorLockToolHandler(_deviceServiceMock.Object, true); // shouldLock = true
        var doorName = "Front Door";
        var door = new DoorLock(Guid.NewGuid(), doorName, "Entrance");
        door.Lock(); // Already Locked

        var arguments = new Dictionary<string, JsonElement>
        {
            { "name", JsonDocument.Parse($"\"{doorName}\"").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, DeviceType.DoorLock, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { door });

        // Act
        var result = await handler.HandleAsync(arguments);

        // Assert
        Assert.Contains($"{doorName} was already locked", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
