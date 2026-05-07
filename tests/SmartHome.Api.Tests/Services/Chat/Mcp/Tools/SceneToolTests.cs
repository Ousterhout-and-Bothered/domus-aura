using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Scene;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class SceneToolTests
{
    private readonly Mock<ISceneService> _sceneServiceMock;
    private readonly SceneTool _tool;

    public SceneToolTests()
    {
        _sceneServiceMock = new Mock<ISceneService>();
        _tool = new SceneTool(_sceneServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteSceneAsync_ReturnsErrorMessage_WhenNameIsMissing()
    {
        // Act
        var result = await _tool.ExecuteSceneAsync(string.Empty);

        // Assert
        Assert.Equal("I need a scene name to execute.", result);
    }

    [Fact]
    public async Task ExecuteSceneAsync_ReturnsErrorMessage_WhenSceneNotFound()
    {
        // Arrange
        _sceneServiceMock.Setup(s => s.GetAllScenesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceScene>());

        // Act
        var result = await _tool.ExecuteSceneAsync("Movie Night");

        // Assert
        Assert.Contains("I could not find a scene named Movie Night", result);
    }

    [Fact]
    public async Task ExecuteSceneAsync_ExecutesScene_WhenValidNameProvided()
    {
        // Arrange
        var sceneName = "Good Night";
        var sceneId = Guid.NewGuid();
        // Use reflection to create DeviceScene since constructor might be tricky with Guard and I don't want to overcomplicate
        // Actually, looking at DeviceScene.cs, it has a public constructor.
        var scene = new DeviceScene(sceneId, sceneName, new[] { SceneAction.ForGroup(SmartHome.Domain.Device.DeviceType.Light, null, "SetPower", 0, "Off") });

        _sceneServiceMock.Setup(s => s.GetAllScenesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceScene> { scene });

        var executionResult = new SceneExecutionResult(sceneId, sceneName, new List<SceneExecutionEntry>
        {
            new SceneExecutionEntry(Guid.NewGuid(), new CommandResult(Guid.NewGuid(), "Light", SmartHome.Domain.Device.DeviceType.Light, "SetPower", "Off", true, null), 0)
        });

        _sceneServiceMock.Setup(s => s.ExecuteSceneAsync(sceneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionResult);

        // Act
        var result = await _tool.ExecuteSceneAsync(sceneName);

        // Assert
        Assert.Contains("Executed scene 'Good Night'", result);
        Assert.Contains("1 actions completed", result);
        _sceneServiceMock.Verify(s => s.ExecuteSceneAsync(sceneId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
