using System.Text.Json;
using Moq;
using SmartHome.Api.Services.Chat.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Api.Tests.Services.Chat.Tools;

public class LightColorToolHandlerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly LightColorToolHandler _handler;

    public LightColorToolHandlerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _handler = new LightColorToolHandler(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "color", JsonDocument.Parse("\"#FF0000\"").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("I need a location to set light color.", result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenColorIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse("\"Living Room\"").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("Please provide a valid color.", result);
    }

    [Fact]
    public async Task HandleAsync_SetsColor_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Living Room";
        var color = "#FF0000";
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        light.TurnOn();

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "color", JsonDocument.Parse($"\"{color}\"").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Light, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { light });

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Contains("Set the Living Room light color to red", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(light.Id, "SetColor", color, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReportsPoweredOff_WhenLightIsOff()
    {
        // Arrange
        var location = "Living Room";
        var color = "#FF0000";
        var light = new Light(Guid.NewGuid(), "Main Light", location);
        // light is off by default

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "color", JsonDocument.Parse($"\"{color}\"").RootElement }
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
