using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Tests.Device;

public class ThermostatTests
{
    private static Thermostat CreateThermostat() => new("Test Thermostat", "Living Room");

    [Fact]
    public void TurnOn_OffToIdle_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        // Default is 72/72, so it should be Idle
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void TurnOn_OffToHeating_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65); // Ambient can be set while off
        thermostat.TurnOn();
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void TurnOff_HeatingToOff_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65);
        thermostat.TurnOn();
        thermostat.TurnOff();
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }

    [Fact]
    public void TurnOn_HeatingToHeating_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.SetAmbientTemperature(65);
        thermostat.TurnOn();
        Assert.Equal(ThermostatState.Heating, thermostat.State);
        
        // This should NOT throw because TurnOn calls TransitionTo(Idle)
        // and Heating -> Idle is allowed.
        thermostat.TurnOn();
        
        // Then EvaluateState() is called, which transitions it back to Heating.
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void SetMode_WhileActive_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        thermostat.SetMode(ThermostatMode.Cool);
        Assert.Equal(ThermostatMode.Cool, thermostat.Mode);
    }

    [Fact]
    public void SetDesiredTemperature_MinBoundary_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        thermostat.SetDesiredTemperature(60);
        Assert.Equal(60, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_MaxBoundary_Succeeds()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        thermostat.SetDesiredTemperature(80);
        Assert.Equal(80, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_BelowMin_ClampsToMin()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        thermostat.SetDesiredTemperature(59);
        Assert.Equal(60, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_AboveMax_ClampsToMax()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        thermostat.SetDesiredTemperature(81);
        Assert.Equal(80, thermostat.DesiredTemperature);
    }
}