using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class LightColorToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly LightColorTool _tool;

    public LightColorToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new LightColorTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task SetLightColorAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Act
        var result = await _tool.SetLightColorAsync(string.Empty, "#FF0000");

        // Assert
        Assert.Equal("I need a location to set light color.", result);
    }

    [Fact]
    public async Task SetLightColorAsync_SetsColor_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Bedroom";
        var color = "#0000FF"; // Blue
        var light = new Light(Guid.NewGuid(), "Bed Light", location);
        light.TurnOn();

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.SetLightColorAsync(location, color);

        // Assert
        Assert.Contains("Set the Bedroom light color to blue", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(light.Id, "SetColor", color, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetLightColorAsync_ReportsPoweredOff_WhenLightIsOff()
    {
        // Arrange
        var location = "Bedroom";
        var color = "#FF0000";
        var light = new Light(Guid.NewGuid(), "Bed Light", location);
        // light is off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.SetLightColorAsync(location, color);

        // Assert
        Assert.Contains("1 light could not be changed because powered off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
