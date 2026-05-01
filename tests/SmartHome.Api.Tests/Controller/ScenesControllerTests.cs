using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Api.Contracts.Scenes;
using SmartHome.Api.Controller;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Scene;
using Xunit;

namespace SmartHome.Api.Tests.Controller;

public class ScenesControllerTests
{
    private readonly Mock<ISceneService> _sceneServiceMock;
    private readonly ScenesController _controller;

    public ScenesControllerTests()
    {
        _sceneServiceMock = new Mock<ISceneService>();
        _controller = new ScenesController(_sceneServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllScenes()
    {
        // Arrange
        var scenes = new List<DeviceScene>
        {
            new("Morning", [SceneAction.ForGroup(DeviceType.Light, "Kitchen", "TurnOn", 0)])
        };
        _sceneServiceMock.Setup(s => s.GetAllScenesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenes);

        // Act
        var result = await _controller.GetAll(default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<SceneResponse>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal("Morning", response.First().Name);
    }

    [Fact]
    public async Task GetById_ReturnsScene()
    {
        // Arrange
        var sceneId = Guid.NewGuid();
        var scene = new DeviceScene("Night", [SceneAction.ForGroup(DeviceType.DoorLock, null, "Lock", 0)]);
        _sceneServiceMock.Setup(s => s.GetSceneAsync(sceneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scene);

        // Act
        var result = await _controller.GetById(sceneId, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SceneResponse>(okResult.Value);
        Assert.Equal("Night", response.Name);
    }

    [Fact]
    public async Task Create_ReturnsCreatedScene()
    {
        // Arrange
        var request = new SceneRequest("Party",
        [
            new SceneActionRequest(null, DeviceType.Light, "Lounge", "SetColor", "#FF0000")
        ]);
        var scene = new DeviceScene("Party", [SceneAction.ForGroup(DeviceType.Light, "Lounge", "SetColor", 0, "#FF0000")]);

        _sceneServiceMock.Setup(s => s.CreateSceneAsync(It.IsAny<string>(), It.IsAny<IEnumerable<SceneAction>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scene);

        // Act
        var result = await _controller.Create(request, default);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SceneResponse>(createdResult.Value);
        Assert.Equal("Party", response.Name);
    }

    [Fact]
    public async Task Execute_ReturnsExecutionResult()
    {
        // Arrange
        var sceneId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var executionResult = new SceneExecutionResult(sceneId, "Test Scene",
        [
            new SceneExecutionEntry(deviceId, new CommandResult(deviceId, "Test Light", DeviceType.Light, "TurnOn", "On", true, null), 0)
        ]);

        _sceneServiceMock.Setup(s => s.ExecuteSceneAsync(sceneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // Act
        var result = await _controller.Execute(sceneId, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SceneExecutionResponse>(okResult.Value);
        Assert.Equal(sceneId, response.SceneId);
        Assert.Equal(1, response.Summary.Succeeded);
        Assert.Single(response.Results);
        Assert.Equal("changed", response.Results[0].Status);
    }
}
