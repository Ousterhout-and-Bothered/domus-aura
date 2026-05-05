using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Api.Controller;
using SmartHome.Domain.Simulation;
using Xunit;

namespace SmartHome.Api.Tests.Controller;

public class LocationsControllerTests
{
    private readonly Mock<ISimulationService> _simulationServiceMock;
    private readonly LocationsController _controller;

    public LocationsControllerTests()
    {
        _simulationServiceMock = new Mock<ISimulationService>();
        _controller = new LocationsController(_simulationServiceMock.Object);
    }

    [Fact]
    public async Task SetAmbientTemperature_ReturnsOk()
    {
        // Arrange
        var location = "Living Room";
        var request = new SetAmbientTemperatureRequest(72);

        // Act
        var result = await _controller.SetAmbientTemperature(location, request, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SetAmbientTemperatureResponse>(okResult.Value);
        Assert.Equal(location, response.Location);
        Assert.Equal(72, response.AmbientTemperature);
        _simulationServiceMock.Verify(s => s.SetAmbientTemperatureAsync(location, 72, It.IsAny<CancellationToken>()), Times.Once);
    }
}
