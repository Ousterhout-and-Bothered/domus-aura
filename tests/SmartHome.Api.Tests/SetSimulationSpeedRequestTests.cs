using SmartHome.Api.Controller;
using System.Text.Json;

namespace SmartHome.Api.Tests;

public class SetSimulationSpeedRequestTests
{
    [Fact]
    public void GetSpeedValue_ShouldReturnInt_WhenInputIsFive()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(5);

        // Act
        var result = request.GetSpeedValue();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetSpeedValue_ShouldReturnNull_WhenInputIsNull()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest(null);

        // Act
        var result = request.GetSpeedValue();

        // Assert
        Assert.Null(result);
    }
}
