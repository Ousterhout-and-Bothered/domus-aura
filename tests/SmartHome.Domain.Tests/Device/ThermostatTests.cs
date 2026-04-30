using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Tests.Device;

public class ThermostatTests
{
    private static Thermostat CreateThermostat() => new("Test Thermostat", "Living Room");

    [Fact]
    public void TurnOn_OffToIdle_Succeeds()
    {
        // Arrange — default ambient (72) and desired (72) match, so the thermostat is Idle.
        var thermostat = CreateThermostat();

        // Act
        thermostat.TurnOn();

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void TurnOn_OffToHeating_Succeeds()
    {
        // Arrange — ambient below desired triggers Heating.
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65);

        // Act
        thermostat.TurnOn();

        // Assert
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void TurnOff_HeatingToOff_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65);
        thermostat.TurnOn();

        // Act
        thermostat.TurnOff();

        // Assert
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }

    [Fact]
    public void TurnOn_HeatingToHeating_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65);
        thermostat.TurnOn();
        Assert.Equal(ThermostatState.Heating, thermostat.State);

        // Act — TurnOn while already-Heating should NOT throw because TurnOn calls
        // TransitionTo(Idle) and Heating -> Idle is allowed; EvaluateState() then
        // transitions back to Heating since ambient is still below desired.
        thermostat.TurnOn();

        // Assert
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void SetMode_WhileActive_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.TurnOn();

        // Act
        thermostat.SetMode(ThermostatMode.Cool);

        // Assert
        Assert.Equal(ThermostatMode.Cool, thermostat.Mode);
    }

    [Fact]
    public void SetDesiredTemperature_MinBoundary_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.TurnOn();

        // Act
        thermostat.SetDesiredTemperature(60);

        // Assert
        Assert.Equal(60, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_MaxBoundary_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.TurnOn();

        // Act
        thermostat.SetDesiredTemperature(80);

        // Assert
        Assert.Equal(80, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_BelowMin_ClampsToMin()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.TurnOn();

        // Act
        thermostat.SetDesiredTemperature(59);

        // Assert
        Assert.Equal(60, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_AboveMax_ClampsToMax()
    {
        // Arrange
        var thermostat = CreateThermostat();
        thermostat.TurnOn();

        // Act
        thermostat.SetDesiredTemperature(81);

        // Assert
        Assert.Equal(80, thermostat.DesiredTemperature);
    }
}