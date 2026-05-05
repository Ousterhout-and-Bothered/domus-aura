using Moq;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Events;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Scene;
using SmartHome.Infrastructure.Device.Service;
using SmartHome.Domain.Device.Commands;
using Xunit;

namespace SmartHome.Infrastructure.Tests.Device;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _repositoryMock = new();
    private readonly Mock<IDeviceFactory> _factoryMock = new();
    private readonly Mock<IDeviceCommandFactory> _commandFactoryMock = new();
    private readonly Mock<IDeviceEventNotifier> _notifierMock = new();
    private readonly Mock<ISceneRepository> _sceneRepositoryMock = new();
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        _service = new DeviceService(
            _repositoryMock.Object,
            _factoryMock.Object,
            _commandFactoryMock.Object,
            _notifierMock.Object,
            _sceneRepositoryMock.Object);
    }

    // --- Device Creation via Factory ---

    [Theory]
    [InlineData(DeviceType.Light, typeof(Light))]
    [InlineData(DeviceType.Fan, typeof(Fan))]
    [InlineData(DeviceType.Thermostat, typeof(Thermostat))]
    [InlineData(DeviceType.DoorLock, typeof(DoorLock))]
    public void DeviceFactory_Create_ReturnsCorrectTypeForEachType(DeviceType type, Type expectedType)
    {
        // Arrange
        var realFactory = new DeviceFactory(new IDeviceBuilder[]
        {
            new LightBuilder(),
            new FanBuilder(),
            new DoorLockBuilder(),
            new ThermostatBuilder(new ThermostatStrategyProvider())
        });

        // Act
        var device = realFactory.Create("Test", "Location", type);

        // Assert
        Assert.IsType(expectedType, device);
    }

    // --- Device Registration ---

    [Fact]
    public async Task RegisterDeviceAsync_ValidRequest_CreatesDeviceWithDefaultState()
    {
        // Arrange
        var name = "New Light";
        var location = "Kitchen";
        var type = DeviceType.Light;
        var device = new Light(name, location);

        _factoryMock.Setup(f => f.Create(name, location, type)).Returns(device);
        _repositoryMock.Setup(r => r.ThermostatExistsAtLocationAsync(location, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.RegisterDeviceAsync(name, location, type);

        // Assert
        Assert.Equal(device, result);
        _repositoryMock.Verify(r => r.AddAsync(device, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.PublishAsync(device, DeviceChangeType.Created, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Device Removal ---

    [Fact]
    public async Task RemoveDeviceAsync_ExistingDevice_RemovesAndPublishesEvent()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new Light(deviceId, "Lamp", "Bedroom");

        _repositoryMock.Setup(r => r.GetByIdReadOnlyAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        _repositoryMock.Setup(r => r.RemoveByIdAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sceneRepositoryMock.Setup(s => s.RemoveActionsForDeviceAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        // Act
        await _service.RemoveDeviceAsync(deviceId);

        // Assert
        _repositoryMock.Verify(r => r.RemoveByIdAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.PublishDeletedAsync(deviceId, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Thermostat Invariant ---

    [Fact]
    public async Task RegisterDeviceAsync_SecondThermostatInSameLocation_ThrowsDuplicateThermostatException()
    {
        // Arrange
        var location = "Living Room";
        _repositoryMock.Setup(r => r.ThermostatExistsAtLocationAsync(location, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateThermostatException>(() =>
            _service.RegisterDeviceAsync("Thermostat 2", location, DeviceType.Thermostat));
    }
    // --- Operations Recording ---

    [Fact]
    public async Task ExecuteCommandAsync_ValidCommand_RecordsOperationInHistory()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new Light(deviceId, "Lamp", "Bedroom");
        device.TurnOn(); // Must be on to change brightness

        _repositoryMock.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        
        var commandMock = new Mock<IDeviceCommand>();
        _commandFactoryMock.Setup(f => f.Create("setBrightness", "50", device)).Returns(commandMock.Object);

        // Act
        await _service.ExecuteCommandAsync(deviceId, "setBrightness", "50");

        // Assert
        _repositoryMock.Verify(r => r.LogActionAsync(
            deviceId,
            "setBrightness: 50",
            It.IsAny<CancellationToken>()), Times.Once);
        
        commandMock.Verify(c => c.Execute(), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- History Retrieval ---

    [Fact]
    public async Task GetDeviceHistoryAsync_ExistingDevice_ReturnsHistoryInChronologicalOrder()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new Light(deviceId, "Lamp", "Bedroom");
        
        // We can't easily mock the timestamp since it's set in constructor to DateTime.UtcNow,
        // but we can control the order by creating them sequentially.
        var history1 = new CommandHistory(deviceId, "TurnOn");
        await Task.Delay(10); // Ensure different timestamps
        var history2 = new CommandHistory(deviceId, "SetBrightness: 50");
        
        var history = new List<CommandHistory> { history1, history2 };

        _repositoryMock.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        _repositoryMock.Setup(r => r.GetHistoryAsync(deviceId, It.IsAny<CancellationToken>())).ReturnsAsync(history);

        // Act
        var result = await _service.GetDeviceHistoryAsync(deviceId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("TurnOn", result[0].Operation);
        Assert.Equal("SetBrightness: 50", result[1].Operation);
        Assert.True(result[0].Timestamp < result[1].Timestamp);
    }
}
