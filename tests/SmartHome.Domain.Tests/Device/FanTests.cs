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
        var fan = CreateFan();
        fan.TurnOn();
        Assert.Equal(PowerState.On, fan.PowerState);
    }

    [Fact]
    public void TurnOff_OnToOff_SetsStateToOff()
    {
        var fan = CreateFan();
        fan.TurnOn();
        fan.TurnOff();
        Assert.Equal(PowerState.Off, fan.PowerState);
    }

    [Fact]
    public void TurnOn_OnToOn_ThrowsInvalidOperationException()
    {
        var fan = CreateFan();
        fan.TurnOn();
        Assert.Throws<InvalidDomainOperationException>(() => fan.TurnOn());
    }

    [Fact]
    public void TurnOff_OffToOff_ThrowsInvalidOperationException()
    {
        var fan = CreateFan();
        Assert.Throws<InvalidDomainOperationException>(() => fan.TurnOff());
    }

    [Fact]
    public void SetSpeed_ValidSpeedWhileOn_UpdatesSpeed()
    {
        var fan = CreateFan();
        fan.TurnOn();
        fan.SetSpeed(FanSpeed.High);
        Assert.Equal(FanSpeed.High, fan.Speed);
    }

    [Fact]
    public void SetSpeed_ValidSpeedWhileOff_ThrowsInvalidOperationException()
    {
        var fan = CreateFan();
        Assert.Throws<InvalidDomainOperationException>(() => fan.SetSpeed(FanSpeed.High));
    }

    [Fact]
    public void SetSpeed_InvalidEnumValue_ThrowsArgumentException()
    {
        var fan = CreateFan();
        fan.TurnOn();
        Assert.Throws<InvalidDomainArgumentException>(() => fan.SetSpeed((FanSpeed)99));
    }

    [Fact]
    public void Speed_SurvivePowerCycle_Retained()
    {
        var fan = CreateFan();
        fan.TurnOn();
        fan.SetSpeed(FanSpeed.Low);
        
        fan.TurnOff();
        fan.TurnOn();
        
        Assert.Equal(FanSpeed.Low, fan.Speed);
    }
}