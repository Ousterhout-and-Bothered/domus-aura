using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class ThermostatTempToolTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly ThermostatTempTool _tool;

    public ThermostatTempToolTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _tool = new ThermostatTempTool(_deviceServiceMock.Object);
    }

    private Thermostat CreateThermostat(Guid id, string name, string location)
    {
        return new Thermostat(id, name, location, new ThermostatStrategyProvider());
    }

    [Fact]
    public async Task SetThermostatTemperatureAsync_SetsTempAndPowersOn_WhenOff()
    {
        // Arrange
        var location = "Basement";
        var temp = 75;
        var thermostat = CreateThermostat(Guid.NewGuid(), "Basement Stat", location);
        // By default State is Off.

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _tool.SetThermostatTemperatureAsync(location, temp);

        // Assert
        Assert.Contains("Set the Basement thermostat to 75°F", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetPower", "On", It.IsAny<CancellationToken>()), Times.Once);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetDesiredTemperature", "75", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetThermostatTemperatureAsync_ReportsAlreadyCorrect_WhenTempMatches()
    {
        // Arrange
        var location = "Basement";
        var temp = 70;
        var thermostat = CreateThermostat(Guid.NewGuid(), "Basement Stat", location);
        thermostat.TurnOn();
        // Default desired is 72 in Thermostat(Guid, string, string) but 70 in 
        // Thermostat(Guid, string, string, IThermostatStrategyProvider).
        // Let's force it to 70 and make sure state evaluation happens.
        thermostat.SetDesiredTemperature(70);

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        _deviceServiceMock.Invocations.Clear();

        // Act
        var result = await _tool.SetThermostatTemperatureAsync(location, temp);

        // Assert
        Assert.Contains("The Basement thermostat was already set to 70°F", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
