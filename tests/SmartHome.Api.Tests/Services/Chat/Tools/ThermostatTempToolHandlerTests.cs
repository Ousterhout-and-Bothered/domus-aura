using System.Text.Json;
using Moq;
using SmartHome.Api.Services.Chat.Tools;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Api.Tests.Services.Chat.Tools;

public class ThermostatTempToolHandlerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly ThermostatTempToolHandler _handler;

    public ThermostatTempToolHandlerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _handler = new ThermostatTempToolHandler(_deviceServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenTemperatureIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse("\"Living Room\"").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("Please provide a valid temperature.", result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsErrorMessage_WhenLocationIsMissing()
    {
        // Arrange
        var arguments = new Dictionary<string, JsonElement>
        {
            { "temperature", JsonDocument.Parse("70").RootElement }
        };

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Equal("I need a location to set thermostat temperature.", result);
    }

    [Fact]
    public async Task HandleAsync_SetsTemperature_WhenValidArgumentsProvided()
    {
        // Arrange
        var location = "Living Room";
        var temperature = 70;
        var thermostat = new Thermostat(Guid.NewGuid(), "Main Thermostat", location);

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "temperature", JsonDocument.Parse($"{temperature}").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Contains("Set the Living Room thermostat to 70°F", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetPower", "On", It.IsAny<CancellationToken>()), Times.Once);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(thermostat.Id, "SetDesiredTemperature", "70", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReportsAlreadySet_WhenTemperatureIsSame()
    {
        // Arrange
        var location = "Living Room";
        var temperature = 72; // Default is 72
        var thermostat = new Thermostat(Guid.NewGuid(), "Main Thermostat", location);

        var arguments = new Dictionary<string, JsonElement>
        {
            { "location", JsonDocument.Parse($"\"{location}\"").RootElement },
            { "temperature", JsonDocument.Parse($"{temperature}").RootElement }
        };

        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(location, DeviceType.Thermostat, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { thermostat });

        // Act
        var result = await _handler.HandleAsync(arguments);

        // Assert
        Assert.Contains("already set to 72°F", result);
        _deviceServiceMock.Verify(s => s.ExecuteCommandAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
