using SmartHome.Domain.Device;
using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Tests.Device;

public class FanTests
{
    /// <summary>
    /// Helpers
    /// </summary>
    /// <returns></returns>

    private static Fan CreateFan() => new("Test Fan", "Bedroom");

    private static Fan CreateFanOn()
    {
        var fan = CreateFan();
        fan.TurnOn();
        return fan;
    }

    /// <summary>
    /// Initial state tests
    /// </summary>


    [Fact]
    public void Fan_DefaultState_IsOff()
    {
        // Arrange & Act
        var fan = CreateFan();

        // Assert
        Assert.Equal(PowerState.Off, fan.PowerState);
    }

    [Fact]
    public void Fan_DefaultSpeed_IsMedium()
    {
        // Arrange & Act
        var fan = CreateFan();

        // Assert
        Assert.Equal(FanSpeed.Medium, fan.Speed);
    }

    /// <summary>
    /// Power Tests
    /// </summary>
    /// <returns></returns>

    [Fact]
    public void TurnOn_WhenOff_SetsStateToOn()
    {
        // Arrange
        var fan = CreateFan();

        // Act
        fan.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, fan.PowerState);
    }

    [Fact]
    public void TurnOff_WhenOn_SetsStateToOff()
    {
        // Arrange
        var fan = CreateFanOn();

        // Act
        fan.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, fan.PowerState);
    }

    [Fact]
    public void IsOn_WhenOn_ReturnsTrue()
    {
        // Arrange
        var fan = CreateFanOn();

        // Act
        var result = fan.IsOn();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOn_WhenOff_ReturnsFalse()
    {
        // Arrange
        var fan = CreateFan();

        // Act
        var result = fan.IsOn();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Speed test
    /// </summary>

    [Fact]
    public void SetSpeed_WhenOn_ToLow_UpdatesSpeed()
    {
        // Arrange
        var fan = CreateFanOn();

        // Act
        fan.SetSpeed(FanSpeed.Low);

        // Assert
        Assert.Equal(FanSpeed.Low, fan.Speed);
    }

    [Fact]
    public void SetSpeed_WhenOn_ToHigh_UpdatesSpeed()
    {
        // Arrange
        var fan = CreateFanOn();

        // Act
        fan.SetSpeed(FanSpeed.High);

        // Assert
        Assert.Equal(FanSpeed.High, fan.Speed);
    }

    [Fact]
    public void SetSpeed_WhenOff_Throws()
    {
        // Arrange
        var fan = CreateFan();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => fan.SetSpeed(FanSpeed.High));
    }

    /// <summary>
    /// Setting Retnetion test
    /// </summary>

    [Fact]
    public void Speed_RetainedAfterPowerCycle()
    {
        // Arrange
        var fan = CreateFanOn();
        fan.SetSpeed(FanSpeed.High);

        // Act
        fan.TurnOff();
        fan.TurnOn();

        // Assert
        Assert.Equal(FanSpeed.High, fan.Speed);
    }
}