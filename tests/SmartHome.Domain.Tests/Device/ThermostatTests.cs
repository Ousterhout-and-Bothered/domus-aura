using SmartHome.Domain.Device;
using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Tests.Device;

public class ThermostatTests
{
    /// <summary>
    /// Helpers
    /// </summary>

    private static Thermostat CreateThermostat() => new("Test Thermostat", "Living Room");

    private static Thermostat CreateThermostatOn()
    {
        var thermostat = CreateThermostat();
        thermostat.TurnOn();
        return thermostat;
    }

    /// <summary>
    /// Initial state tests
    /// </summary>
    /// <returns></returns>

    [Fact]
    public void Thermostat_DefaultState_IsOff()
    {
        // Arrange & Act
        var thermostat = CreateThermostat();

        // Assert
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }

    [Fact]
    public void Thermostat_DefaultMode_IsAuto()
    {
        // Arrange & Act
        var thermostat = CreateThermostat();

        // Assert
        Assert.Equal(ThermostatMode.Auto, thermostat.Mode);
    }

    [Fact]
    public void Thermostat_DefaultTemperature_Is72()
    {
        // Arrange & Act
        var thermostat = CreateThermostat();

        // Assert
        Assert.Equal(72, thermostat.DesiredTemperature);
        Assert.Equal(72, thermostat.AmbientTemperature);
    }

    /// <summary>
    /// Power tests
    /// </summary>

    [Fact]
    public void TurnOn_WhenOff_TransitionsToIdle()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.TurnOn();

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void TurnOn_WhenAlreadyOn_DoesNothing()
    {
        // Arrange
        var thermostat = CreateThermostatOn();

        // Act
        thermostat.TurnOn();

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void TurnOff_WhenOn_TransitionsToOff()
    {
        // Arrange
        var thermostat = CreateThermostatOn();

        // Act
        thermostat.TurnOff();

        // Assert
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }
    
    /// <summary>
    /// IsOn tests
    /// </summary>

    [Fact]
    public void IsOn_WhenIdle_ReturnsFalse()
    {
        // Arrange
        var thermostat = CreateThermostatOn();

        // Act
        var result = thermostat.IsOn();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOn_WhenHeating_ReturnsTrue()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetAmbientTemperature(65);
        thermostat.SetDesiredTemperature(72);

        // Act
        var result = thermostat.IsOn();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOn_WhenCooling_ReturnsTrue()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Cool);
        thermostat.SetAmbientTemperature(80);
        thermostat.SetDesiredTemperature(72);

        // Act
        var result = thermostat.IsOn();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Heating/Cooling transition tests
    /// </summary>

    [Fact]
    public void SetAmbientTemperature_BelowDesired_InHeatMode_TransitionsToHeating()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(65);

        // Assert
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void SetAmbientTemperature_AboveDesired_InCoolMode_TransitionsToCooling()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Cool);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(80);

        // Assert
        Assert.Equal(ThermostatState.Cooling, thermostat.State);
    }

    [Fact]
    public void SetAmbientTemperature_EqualToDesired_TransitionsToIdle()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetAmbientTemperature(65);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(72);

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void HeatMode_WhenAmbientAboveDesired_StaysIdle()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(80);

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void CoolMode_WhenAmbientBelowDesired_StaysIdle()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Cool);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(65);

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    /// <summary>
    /// Auto mode tests
    /// </summary>


    [Fact]
    public void AutoMode_WhenAmbientBelowDesired_TransitionsToHeating()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Auto);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(65);

        // Assert
        Assert.Equal(ThermostatState.Heating, thermostat.State);
    }

    [Fact]
    public void AutoMode_WhenAmbientAboveDesired_TransitionsToCooling()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Auto);
        thermostat.SetDesiredTemperature(72);

        // Act
        thermostat.SetAmbientTemperature(80);

        // Assert
        Assert.Equal(ThermostatState.Cooling, thermostat.State);
    }

    /// <summary>
    /// Tick
    /// </summary>

    [Fact]
    public void Tick_WhenHeating_IncrementsAmbientTemperature()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetDesiredTemperature(72);
        thermostat.SetAmbientTemperature(65);

        // Act
        thermostat.Tick();

        // Assert
        Assert.Equal(66, thermostat.AmbientTemperature);
    }

    [Fact]
    public void Tick_WhenCooling_DecrementsAmbientTemperature()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Cool);
        thermostat.SetDesiredTemperature(72);
        thermostat.SetAmbientTemperature(80);

        // Act
        thermostat.Tick();

        // Assert
        Assert.Equal(79, thermostat.AmbientTemperature);
    }

    [Fact]
    public void Tick_WhenAmbientReachesDesired_TransitionsToIdle()
    {
        // Arrange
        var thermostat = CreateThermostatOn();
        thermostat.SetMode(ThermostatMode.Heat);
        thermostat.SetDesiredTemperature(72);
        thermostat.SetAmbientTemperature(71);

        // Act
        thermostat.Tick();

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    [Fact]
    public void Tick_WhenOff_DoesNothing()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.Tick();

        // Assert
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }

    [Fact]
    public void Tick_WhenIdle_DoesNothing()
    {
        // Arrange
        var thermostat = CreateThermostatOn();

        // Act
        thermostat.Tick();

        // Assert
        Assert.Equal(ThermostatState.Idle, thermostat.State);
    }

    /// <summary>
    /// Temperature boundary test
    /// </summary>

    [Fact]
    public void SetDesiredTemperature_AtMinBoundary_Clamps()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.SetDesiredTemperature(59);

        // Assert
        Assert.Equal(60, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_AtMaxBoundary_Clamps()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.SetDesiredTemperature(81);

        // Assert
        Assert.Equal(80, thermostat.DesiredTemperature);
    }

    [Fact]
    public void SetDesiredTemperature_WithinRange_Succeeds()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.SetDesiredTemperature(72);

        // Assert
        Assert.Equal(72, thermostat.DesiredTemperature);
    }

    /// <summary>
    /// Off state tests
    /// </summary>
    /// <returns></returns>

    [Fact]
    public void SetAmbientTemperature_WhenOff_DoesNotTriggerStateChange()
    {
        // Arrange
        var thermostat = CreateThermostat();

        // Act
        thermostat.SetAmbientTemperature(65);

        // Assert
        Assert.Equal(ThermostatState.Off, thermostat.State);
    }
}