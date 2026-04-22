using SmartHome.Api.Controller;
using System.Text.Json;
using Xunit;

namespace SmartHome.Api.Tests;

public class SetSimulationSpeedRequestTests
{
    [Fact]
    public void GetSpeedValue_ShouldReturnInt_WhenInputIsStringFive()
    {
        // Arrange
        var request = new SetSimulationSpeedRequest("5");

        // Act
        var result = request.GetSpeedValue();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetSpeedValue_ShouldReturnInt_WhenInputIsJsonElementStringFive()
    {
        // Arrange
        var json = "{\"speed\": \"5\"}";
        var document = JsonDocument.Parse(json);
        var speedElement = document.RootElement.GetProperty("speed");
        var request = new SetSimulationSpeedRequest(speedElement);

        // Act
        var result = request.GetSpeedValue();

        // Assert
        Assert.Equal(5, result);
    }
}
