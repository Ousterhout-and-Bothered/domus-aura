using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Fan;

namespace SmartHome.Domain.Tests.Device;

public class FanTests
{
    private static Fan CreateFan() => new("Test Fan", "Bedroom");

    [Fact]
    public void TurnOn_OffToOn_SetsStateToOn()
    {
        // Arrange
        var fan = CreateFan();

        // Act
        fan.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, fan.PowerState);
    }

    [Fact]
    public void TurnOff_OnToOff_SetsStateToOff()
    {
        // Arrange
        var fan = CreateFan();
        fan.TurnOn();

        // Act
        fan.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, fan.PowerState);
    }

    [Fact]
    public void TurnOn_OnToOn_IsNoOp()
    {
        // Arrange — TurnOn on an already-on device is idempotent (early-return in PoweredDevice).
        var fan = CreateFan();
        fan.TurnOn();

        // Act
        fan.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, fan.PowerState);
    }

    [Fact]
    public void TurnOff_OffToOff_IsNoOp()
    {
        // Arrange — TurnOff on an already-off device is idempotent.
        var fan = CreateFan();

        // Act
        fan.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, fan.PowerState);
    }

    [Fact]
    public void SetSpeed_ValidSpeedWhileOn_UpdatesSpeed()
    {
        // Arrange
        var fan = CreateFan();
        fan.TurnOn();

        // Act
        fan.SetSpeed(FanSpeed.High);

        // Assert
        Assert.Equal(FanSpeed.High, fan.Speed);
    }

    [Fact]
    public void SetSpeed_InvalidEnumValue_ThrowsInvalidDomainArgument()
    {
        // Arrange
        var fan = CreateFan();
        fan.TurnOn();

        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() => fan.SetSpeed((FanSpeed)99));
    }

    [Fact]
    public void Speed_SurvivePowerCycle_Retained()
    {
        // Arrange — power cycle should preserve speed (only Reset clears it).
        var fan = CreateFan();
        fan.TurnOn();
        fan.SetSpeed(FanSpeed.Low);

        // Act
        fan.TurnOff();
        fan.TurnOn();

        // Assert
        Assert.Equal(FanSpeed.Low, fan.Speed);
    }

    [Fact]
    public void ResetToDefaults_RestoresFactoryDefaults()
    {
        // Arrange — pin the contract that Reset (unlike a power cycle) clears settings.
        var fan = CreateFan();
        fan.TurnOn();
        fan.SetSpeed(FanSpeed.High);

        // Act
        fan.ResetToDefaults();

        // Assert
        Assert.Equal(PowerState.Off, fan.PowerState);
        Assert.Equal(FanSpeed.Medium, fan.Speed);
    }
}