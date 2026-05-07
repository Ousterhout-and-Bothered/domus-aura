using Moq;
using SmartHome.Api.Controller;
using SmartHome.Api.Validation;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Tests.Validation;

public class ValidationLogicTests
{
    private readonly SetSimulationSpeedRequestValidator _validator;
    private readonly Mock<ISimulationSpeedRegistry> _registryMock;

    public ValidationLogicTests()
    {
        _registryMock = new Mock<ISimulationSpeedRegistry>();
        _registryMock.Setup(r => r.AllowedSpeeds).Returns(new HashSet<SimulationSpeed> { SimulationSpeed.X1, SimulationSpeed.X2 });
        _registryMock.Setup(r => r.IsAllowed(It.IsAny<SimulationSpeed>()))
                     .Returns((SimulationSpeed s) => s == SimulationSpeed.X1 || s == SimulationSpeed.X2);

        _validator = new SetSimulationSpeedRequestValidator(_registryMock.Object);
    }

    [Fact]
    public void Request_MissingFields_ReturnsValidationError()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Request_NegativeValue_ReturnsValidationError()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(-1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Request_OutOfRangeValues_ReturnsValidationError()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(100);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }
}
