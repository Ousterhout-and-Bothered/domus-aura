using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class ThermostatModeToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly ThermostatModeTool _tool;

    public ThermostatModeToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new ThermostatModeTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task SetThermostatModeAsync_ReturnsErrorMessage_WhenModeIsInvalid()
    {
        // Act
        var result = await _tool.SetThermostatModeAsync("Living Room", "ExtremeHeat");

        // Assert
        Assert.Equal("Please provide a valid mode: Heat, Cool, or Auto.", result);
    }

    [Fact]
    public async Task SetThermostatModeAsync_SetsMode_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Hallway";
        var mode = "Heat";
        var thermostat = new Thermostat(Guid.NewGuid(), "Main Stat", location);
        thermostat.TurnOn();
        // Default mode is Auto

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _tool.SetThermostatModeAsync(location, mode);

        // Assert
        Assert.Contains("Set the Hallway thermostat to Heat mode", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetMode", "Heat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetThermostatModeAsync_ReportsAlreadyCorrect_WhenModeMatches()
    {
        // Arrange
        var location = "Hallway";
        var mode = "Auto";
        var thermostat = new Thermostat(Guid.NewGuid(), "Main Stat", location);
        thermostat.TurnOn();
        // thermostat.Mode is Auto by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _tool.SetThermostatModeAsync(location, mode);

        // Assert
        Assert.Contains("The Hallway thermostat was already in Auto mode", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
