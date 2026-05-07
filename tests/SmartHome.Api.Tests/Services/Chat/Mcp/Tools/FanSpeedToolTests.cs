using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Fan;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class FanSpeedToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly FanSpeedTool _tool;

    public FanSpeedToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new FanSpeedTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task SetFanSpeedAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Act
        var result = await _tool.SetFanSpeedAsync(string.Empty, "High");

        // Assert
        Assert.Equal("I need a location to set fan speed.", result);
    }

    [Fact]
    public async Task SetFanSpeedAsync_ReturnsErrorMessage_WhenSpeedIsInvalid()
    {
        // Act
        var result = await _tool.SetFanSpeedAsync("Living Room", "Turbo");

        // Assert
        Assert.Equal("Please provide a valid fan speed: Low, Medium, or High.", result);
    }

    [Fact]
    public async Task SetFanSpeedAsync_SetsSpeed_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Living Room";
        var speed = "High";
        var fan = new Fan(Guid.NewGuid(), "Main Fan", location);
        fan.TurnOn();

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Fan, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { fan });

        // Act
        var result = await _tool.SetFanSpeedAsync(location, speed);

        // Assert
        Assert.Contains("Set the Living Room fan to High speed", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(fan.Id, "SetSpeed", "High", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFanSpeedAsync_ReportsPoweredOff_WhenFanIsOff()
    {
        // Arrange
        var location = "Living Room";
        var speed = "Medium";
        var fan = new Fan(Guid.NewGuid(), "Main Fan", location);
        // fan is off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Fan, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { fan });

        // Act
        var result = await _tool.SetFanSpeedAsync(location, speed);

        // Assert
        Assert.Contains("1 fan could not be changed because powered off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
