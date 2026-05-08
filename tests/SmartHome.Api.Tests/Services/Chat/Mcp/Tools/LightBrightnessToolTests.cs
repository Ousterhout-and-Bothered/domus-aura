using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class LightBrightnessToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly LightBrightnessTool _tool;

    public LightBrightnessToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new LightBrightnessTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task SetLightBrightnessAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Act
        var result = await _tool.SetLightBrightnessAsync(string.Empty, 50);

        // Assert
        Assert.Equal("I need a location to set light brightness.", result);
    }

    [Fact]
    public async Task SetLightBrightnessAsync_SetsBrightness_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Living Room";
        var brightness = 75;
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        light.TurnOn();

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.SetLightBrightnessAsync(location, brightness);

        // Assert
        Assert.Contains("Set the Living Room light brightness to 75%", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(light.Id, "SetBrightness", "75", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetLightBrightnessAsync_ReportsPoweredOff_WhenLightIsOff()
    {
        // Arrange
        var location = "Living Room";
        var brightness = 50;
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        // light is off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _tool.SetLightBrightnessAsync(location, brightness);

        // Assert
        Assert.Contains("1 light could not be changed because powered off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
