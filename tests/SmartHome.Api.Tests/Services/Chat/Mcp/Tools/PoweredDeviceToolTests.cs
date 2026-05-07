using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class PoweredDeviceToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly PoweredDeviceTool _tool;

    public PoweredDeviceToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new PoweredDeviceTool(_deviceServiceMock.Object, DeviceType.Light, "light");
    }

    [Fact]
    public async Task TurnOnAsync_TurnsOnDevice_WhenDeviceIsOff()
    {
        // Arrange
        var location = "Kitchen";
        var light = new Light(Guid.NewGuid(), "Overhead", location);
        // light is off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.TurnOnAsync(location);

        // Assert
        Assert.Contains("Turned on 1 light in Kitchen", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(light.Id, "SetPower", "On", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TurnOffAsync_ReportsAlreadyOff_WhenDeviceIsOff()
    {
        // Arrange
        var location = "Kitchen";
        var light = new Light(Guid.NewGuid(), "Overhead", location);
        // light is off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.TurnOffAsync(location);

        // Assert
        Assert.Contains("The light in Kitchen was already off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
