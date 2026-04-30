using Moq;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;
using SmartHome.Infrastructure.Scene;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Common.Exceptions;
using Xunit;

namespace SmartHome.Infrastructure.Tests.Scene;

public class SceneServiceTests
{
    private readonly Mock<ISceneRepository> _sceneRepositoryMock;
    private readonly Mock<IDeviceRepository> _deviceRepositoryMock;
    private readonly Mock<ISceneResolver> _resolverMock;
    private readonly SceneService _service;

    public SceneServiceTests()
    {
        _sceneRepositoryMock = new Mock<ISceneRepository>();
        _deviceRepositoryMock = new Mock<IDeviceRepository>();
        _resolverMock = new Mock<ISceneResolver>();
        _service = new SceneService(
            _sceneRepositoryMock.Object,
            _deviceRepositoryMock.Object,
            _resolverMock.Object);
    }

    [Fact]
    public async Task GetSceneAsync_WhenNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        var sceneId = Guid.NewGuid();
        _sceneRepositoryMock.Setup(r => r.GetByIdAsync(sceneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceScene)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _service.GetSceneAsync(sceneId));
    }

    [Fact]
    public async Task ExecuteSceneAsync_LogsHistoryAndSavesChanges()
    {
        // Arrange
        var sceneId = Guid.NewGuid();
        var scene = new DeviceScene("Night", [SceneAction.ForGroup(SmartHome.Domain.Device.DeviceType.DoorLock, null, "Lock", 0)]);
        var deviceId = Guid.NewGuid();

        var composite = new CompositeCommand();
        var mockCommand = new Mock<IDeviceCommand>();
        mockCommand.Setup(c => c.Execute()).Returns(new CommandResult(
            DeviceId: Guid.Empty,
            DeviceName: "Test Device",
            DeviceType: SmartHome.Domain.Device.DeviceType.DoorLock,
            Operation: "Lock",
            Value: null,
            Success: true,
            Message: null));
        mockCommand.Setup(c => c.OperationName).Returns("Lock");
        composite.Add(mockCommand.Object);

        var resolved = new ResolvedScene(composite, [deviceId], new[] { 0 });

        _sceneRepositoryMock.Setup(r => r.GetByIdAsync(sceneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scene);
        _resolverMock.Setup(r => r.ResolveAsync(scene, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        // Act
        var result = await _service.ExecuteSceneAsync(sceneId);

        // Assert
        Assert.Equal(scene.Id, result.SceneId);
        _deviceRepositoryMock.Verify(r => r.LogActionAsync(deviceId, It.Is<string>(s => s.Contains("Lock")), It.IsAny<CancellationToken>()), Times.Once);
        _deviceRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSceneAsync_PersistsScene()
    {
        // Arrange
        var name = "Morning";
        var actions = new List<SceneAction> { SceneAction.ForGroup(SmartHome.Domain.Device.DeviceType.Light, "Kitchen", "TurnOn", 0) };

        // Act
        var result = await _service.CreateSceneAsync(name, actions);

        // Assert
        Assert.Equal(name, result.Name);
        _sceneRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DeviceScene>(), It.IsAny<CancellationToken>()), Times.Once);
        _sceneRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
