using System.Text.Json;
using Moq;
using SmartHome.Api.Services.Chat.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Api.Tests.Services.Chat.Tools;

public class LightBrightnessToolHandlerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly LightBrightnessToolHandler _handler;

    public LightBrightnessToolHandlerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _handler = new LightBrightnessToolHandler(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "brightness", JsonDocument.Parse("50").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("I need a location to set light brightness.", result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenBrightnessIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse("\"Living Room\"").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("I need a valid brightness value.", result);
    }

    [Fact]
    public async Task HandleAsync_SetsBrightness_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Living Room";
        var brightness = 75;
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        light.TurnOn();

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "brightness", JsonDocument.Parse($"{brightness}").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Contains("Set the Living Room light brightness to 75%", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(light.Id, "SetBrightness", "75", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReportsPoweredOff_WhenLightIsOff()
    {
        // Arrange
        var location = "Living Room";
        var brightness = 50;
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        // light is off by default

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "brightness", JsonDocument.Parse($"{brightness}").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Contains("1 light could not be changed because powered off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
