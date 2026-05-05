using Moq;
using SmartHome.Api.Services.Chat.Mcp.Tools;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Tests.Services.Chat.Mcp.Tools;

public class SimulationSpeedToolTests
{
    private readonly Mock<ISimulationService> _simulationServiceMock;
    private readonly Mock<ISimulationSpeedRegistry> _registryMock;
    private readonly SimulationSpeedTool _tool;

    public SimulationSpeedToolTests()
    {
        _simulationServiceMock = new Mock<ISimulationService>();
        _registryMock = new Mock<ISimulationSpeedRegistry>();
        _tool = new SimulationSpeedTool(_simulationServiceMock.Object, _registryMock.Object);
    }

    [Fact]
    public async Task SetSimulationSpeedAsync_ReturnsErrorMessage_WhenSpeedIsInvalid()
    {
        // Act
        var result = await _tool.SetSimulationSpeedAsync(3);

        // Assert
        Assert.Equal("Invalid simulation speed. Allowed values are 1, 2, 5, or 10.", result);
    }

    [Fact]
    public async Task SetSimulationSpeedAsync_SetsSpeed_WhenSpeedIsAllowed()
    {
        // Arrange
        var speed = 5;
        _registryMock.Setup(r => r.IsAllowed(SimulationSpeed.X5)).Returns(true);

        // Act
        var result = await _tool.SetSimulationSpeedAsync(speed);

        // Assert
        Assert.Contains("Simulation speed set to 5x", result);
        _simulationServiceMock.Verify(s => s.SetSpeedAsync(SimulationSpeed.X5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSimulationSpeedAsync_ReturnsErrorMessage_WhenSpeedIsNotAllowedByRegistry()
    {
        // Arrange
        var speed = 10;
        _registryMock.Setup(r => r.IsAllowed(SimulationSpeed.X10)).Returns(false);

        // Act
        var result = await _tool.SetSimulationSpeedAsync(speed);

        // Assert
        Assert.Equal("Invalid simulation speed. Allowed values are 1, 2, 5, or 10.", result);
    }
}
