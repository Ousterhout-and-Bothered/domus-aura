using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Common;

namespace SmartHome.Domain.Tests.Device;

public class LightTests
{
    private static Light CreateLight() => new("Test Light", "Living Room");

    [Fact]
    public void TurnOn_OffToOn_SetsStateToOn()
    {
        var light = CreateLight();
        light.TurnOn();
        Assert.Equal(PowerState.On, light.PowerState);
    }

    [Fact]
    public void TurnOff_OnToOff_SetsStateToOff()
    {
        var light = CreateLight();
        light.TurnOn();
        light.TurnOff();
        Assert.Equal(PowerState.Off, light.PowerState);
    }

    [Fact]
    public void TurnOn_OnToOn_ThrowsInvalidOperationException()
    {
        var light = CreateLight();
        light.TurnOn();
        Assert.Throws<InvalidDomainOperationException>(() => light.TurnOn());
    }

    [Fact]
    public void TurnOff_OffToOff_ThrowsInvalidOperationException()
    {
        var light = CreateLight();
        Assert.Throws<InvalidDomainOperationException>(() => light.TurnOff());
    }

    [Fact]
    public void SetBrightness_ValidValue_UpdatesBrightness()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(50);
        Assert.Equal(50, light.Brightness);
    }

    [Fact]
    public void SetBrightness_MinBoundary_Succeeds()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(10);
        Assert.Equal(10, light.Brightness);
    }

    [Fact]
    public void SetBrightness_MaxBoundary_Succeeds()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(100);
        Assert.Equal(100, light.Brightness);
    }

    [Fact]
    public void SetBrightness_BelowMin_ClampsToMin()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(9);
        Assert.Equal(10, light.Brightness);
    }

    [Fact]
    public void SetBrightness_AboveMax_ClampsToMax()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(101);
        Assert.Equal(100, light.Brightness);
    }

    [Fact]
    public void SetColor_ValidHex_UpdatesColor()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetColor("#FF0000");
        Assert.Equal("#FF0000", light.ColorHex);
    }

    [Fact]
    public void SetColor_InvalidHex_ThrowsArgumentException()
    {
        var light = CreateLight();
        light.TurnOn();
        Assert.Throws<InvalidDomainArgumentException>(() => light.SetColor("invalid"));
    }

    [Fact]
    public void Settings_SurvivePowerCycle_Retained()
    {
        var light = CreateLight();
        light.TurnOn();
        light.SetBrightness(75);
        light.SetColor("#00FF00");
        
        light.TurnOff();
        light.TurnOn();
        
        Assert.Equal(75, light.Brightness);
        Assert.Equal("#00FF00", light.ColorHex);
    }
}