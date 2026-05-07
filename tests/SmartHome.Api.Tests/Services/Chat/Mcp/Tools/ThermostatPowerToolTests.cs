using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class ThermostatPowerToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly ThermostatPowerTool _tool;

    public ThermostatPowerToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new ThermostatPowerTool(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task TurnOnThermostatsAsync_TurnsOn_WhenOff()
    {
        // Arrange
        var location = "Office";
        var thermostat = new Thermostat(Guid.NewGuid(), "Office Stat", location);
        // Off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _tool.TurnOnThermostatsAsync(location);

        // Assert
        Assert.Contains("Turned on the Office thermostat", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetPower", "On", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TurnOffThermostatsAsync_ReportsAlreadyOff_WhenOff()
    {
        // Arrange
        var location = "Office";
        var thermostat = new Thermostat(Guid.NewGuid(), "Office Stat", location);
        // Off by default

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _tool.TurnOffThermostatsAsync(location);

        // Assert
        Assert.Contains("The Office thermostat was already off", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
