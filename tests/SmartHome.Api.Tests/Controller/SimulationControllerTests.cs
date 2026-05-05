using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Api.Contracts;
using SmartHome.Api.Controller;
using SmartHome.Domain.Common;
using SmartHome.Domain.Simulation;
using Xunit;

namespace SmartHome.Api.Tests.Controller;

public class SimulationControllerTests
{
    private readonly Mock<ISimulationService> _simulationServiceMock;
    private readonly Mock<ISimulationSpeedRegistry> _registryMock;
    private readonly SimulationController _controller;

    public SimulationControllerTests()
    {
        _simulationServiceMock = new Mock<ISimulationService>();
        _registryMock = new Mock<ISimulationSpeedRegistry>();
        _controller = new SimulationController(_simulationServiceMock.Object, _registryMock.Object);
    }

    [Fact]
    public void GetSimulationState_ReturnsState()
    {
        // Arrange
        var clock = DateTime.UtcNow;
        _simulationServiceMock.Setup(s => s.Speed).Returns(SimulationSpeed.X1);
        _simulationServiceMock.Setup(s => s.SimulationClock).Returns(clock);

        // Act
        var result = _controller.GetSimulationState();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SimulationStateResponse>(okResult.Value);
        Assert.Equal(1, response.SpeedMultiplier);
        Assert.Equal(clock, response.SimulationClock);
    }

    [Fact]
    public void GetAllowedSpeeds_ReturnsSpeeds()
    {
        // Arrange
        var speeds = new HashSet<SimulationSpeed> { SimulationSpeed.X1, SimulationSpeed.X2 };
        _registryMock.Setup(r => r.AllowedSpeeds).Returns(speeds);

        // Act
        var result = _controller.GetAllowedSpeeds();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AllowedSpeedsResponse>(okResult.Value);
        Assert.Equal(2, response.Speeds.Count);
        Assert.Contains(1, response.Speeds);
        Assert.Contains(2, response.Speeds);
    }

    [Fact]
    public async Task SetSpeed_CallsService()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(5);

        // Act
        var result = await _controller.SetSpeed(request, default);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _simulationServiceMock.Verify(s => s.SetSpeedAsync(SimulationSpeed.X5, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void RejectPathSpeed_ReturnsProblem()
    {
        // Act
        var result = _controller.RejectPathSpeed("10");

        // Assert
        var problemResult = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Reset_CallsService()
    {
        // Act
        var result = await _controller.Reset(default);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _simulationServiceMock.Verify(s => s.ResetAllDevicesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
