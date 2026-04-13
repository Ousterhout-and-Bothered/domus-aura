using SmartHome.Domain.Device;
using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Tests.Device;

public class LightTests
{
    
    private static Light CreateLight() => new("Test Light", "Living Room");

    private static Light CreateLightOn()
    {
        var light = CreateLight();
        light.TurnOn();
        return light;
    }
    
    /// <summary>
    /// Power tests
    /// </summary>
    [Fact]
    public void TurnOn_WhenOff_SetsStateToOn()
    {
        // Arrange
        var light = CreateLight();

        // Act
        light.TurnOn();

        // Assert
        Assert.Equal(PowerState.On, light.PowerState);
    }

    [Fact]
    public void TurnOff_WhenOn_SetsStateToOff()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.TurnOff();

        // Assert
        Assert.Equal(PowerState.Off, light.PowerState);
    }

    [Fact]
    public void IsOn_WhenOn_ReturnsTrue()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        var result = light.IsOn();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOn_WhenOff_ReturnsFalse()
    {
        // Arrange
        var light = CreateLight();

        // Act
        var result = light.IsOn();

        // Assert
        Assert.False(result);
    }
    
    
    /// <summary>
    ///  Brightness tests
    /// </summary>
    [Fact]
    public void SetBrightness_WhenOn_UpdatesBrightness()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.SetBrightness(75);

        // Assert
        Assert.Equal(75, light.Brightness);
    }

    [Fact]
    public void SetBrightness_WhenOff_Throws()
    {
        // Arrange
        var light = CreateLight();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => light.SetBrightness(50));
    }

    [Fact]
    public void SetBrightness_AtMinBoundary_Succeeds()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.SetBrightness(10);

        // Assert
        Assert.Equal(10, light.Brightness);
    }

    [Fact]
    public void SetBrightness_AtMaxBoundary_Succeeds()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.SetBrightness(100);

        // Assert
        Assert.Equal(100, light.Brightness);
    }

    [Fact]
    public void SetBrightness_BelowMin_Throws()
    {
        // Arrange
        var light = CreateLightOn();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => light.SetBrightness(9));
    }

    [Fact]
    public void SetBrightness_AboveMax_Throws()
    {
        // Arrange
        var light = CreateLightOn();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => light.SetBrightness(101));
    }
    
    
    /// <summary>
    /// Color tests
    /// </summary>
    
    [Fact]
    public void SetColor_WhenOn_ValidHex_UpdatesColor()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.SetColor("#FF8800");

        // Assert
        Assert.Equal("#FF8800", light.ColorHex);
    }

    [Fact]
    public void SetColor_WhenOff_Throws()
    {
        // Arrange
        var light = CreateLight();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => light.SetColor("#FF8800"));
    }

    [Fact]
    public void SetColor_InvalidHex_Throws()
    {
        // Arrange
        var light = CreateLightOn();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => light.SetColor("not-a-color"));
    }

    [Fact]
    public void SetColor_EmptyString_Throws()
    {
        // Arrange
        var light = CreateLightOn();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => light.SetColor(""));
    }

    [Fact]
    public void SetColor_StoresAsUppercase()
    {
        // Arrange
        var light = CreateLightOn();

        // Act
        light.SetColor("#ff8800");

        // Assert
        Assert.Equal("#FF8800", light.ColorHex);
    }
    
    
    /// <summary>
    /// Settings Retention tests
    /// </summary>
    
    [Fact]
    public void Brightness_RetainedAfterPowerCycle()
    {
        // Arrange
        var light = CreateLightOn();
        light.SetBrightness(42);

        // Act
        light.TurnOff();
        light.TurnOn();

        // Assert
        Assert.Equal(42, light.Brightness);
    }

    [Fact]
    public void Color_RetainedAfterPowerCycle()
    {
        // Arrange
        var light = CreateLightOn();
        light.SetColor("#FF8800");

        // Act
        light.TurnOff();
        light.TurnOn();

        // Assert
        Assert.Equal("#FF8800", light.ColorHex);
    }
}