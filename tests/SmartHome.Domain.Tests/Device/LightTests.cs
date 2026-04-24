using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Tests.Device;

public class LightTests
{
    private static Light CreateLight() => new("Test Light", "Living Room");

    [Fact]
    public void TurnOn_OffToOn_SetsStateToOn()
    {
        // Arrange
        var light = CreateLight();

        // Act
        light.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, light.PowerState);
    }

    [Fact]
    public void TurnOff_OnToOff_SetsStateToOff()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, light.PowerState);
    }

    [Fact]
    public void TurnOn_OnToOn_IsNoOp()
    {
        // Arrange — TurnOn on an already-on device is idempotent (early-return in PoweredDevice).
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, light.PowerState);
    }

    [Fact]
    public void TurnOff_OffToOff_IsNoOp()
    {
        // Arrange — TurnOff on an already-off device is idempotent.
        var light = CreateLight();

        // Act
        light.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, light.PowerState);
    }

    [Fact]
    public void SetBrightness_ValidValue_UpdatesBrightness()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetBrightness(50);

        // Assert
        Assert.Equal(50, light.Brightness);
    }

    [Fact]
    public void SetBrightness_MinBoundary_Succeeds()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetBrightness(10);

        // Assert
        Assert.Equal(10, light.Brightness);
    }

    [Fact]
    public void SetBrightness_MaxBoundary_Succeeds()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetBrightness(100);

        // Assert
        Assert.Equal(100, light.Brightness);
    }

    [Fact]
    public void SetBrightness_BelowMin_ClampsToMin()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetBrightness(9);

        // Assert
        Assert.Equal(10, light.Brightness);
    }

    [Fact]
    public void SetBrightness_AboveMax_ClampsToMax()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetBrightness(101);

        // Assert
        Assert.Equal(100, light.Brightness);
    }

    [Fact]
    public void SetColor_ValidHex_UpdatesColor()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act
        light.SetColor("#FF0000");

        // Assert
        Assert.Equal("#FF0000", light.ColorHex);
    }

    [Fact]
    public void SetColor_InvalidHex_ThrowsInvalidDomainArgument()
    {
        // Arrange
        var light = CreateLight();
        light.TurnOn();

        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() => light.SetColor("invalid"));
    }

    [Fact]
    public void Settings_SurvivePowerCycle_Retained()
    {
        // Arrange — power cycle should preserve brightness and color (only Reset clears them).
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(75);
        light.SetColor("#00FF00");

        // Act
        light.TurnOff();
        light.TurnOn();

        // Assert
        Assert.Equal(75, light.Brightness);
        Assert.Equal("#00FF00", light.ColorHex);
    }

    [Fact]
    public void ResetToDefaults_RestoresFactoryDefaults()
    {
        // Arrange — pin the contract that Reset (unlike a power cycle) clears settings.
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(75);
        light.SetColor("#00FF00");

        // Act
        light.ResetToDefaults();

        // Assert
        Assert.Equal(PowerState.Off, light.PowerState);
        Assert.Equal(100, light.Brightness);
        Assert.Equal("#FFFFFF", light.ColorHex);
    }
}
