using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class DeviceFactoryTests
{
     private static readonly IDeviceFactory Factory = new DeviceFactory(new IDeviceBuilder[]
{
    new LightBuilder(),
    new FanBuilder(),
    new DoorLockBuilder(),
    new ThermostatBuilder(new ThermostatStrategyProvider())
});

    /// <summary>
    /// Correct type tests
    /// </summary>

    [Fact]
    public void Create_Light_ReturnsLightInstance()
    {
        var device = Factory.Create("Test Light", "Living Room", DeviceType.Light);
        Assert.IsType<Light>(device);
    }

    [Fact]
    public void Create_Fan_ReturnsFanInstance()
    {
        var device = Factory.Create("Test Fan", "Bedroom", DeviceType.Fan);
        Assert.IsType<Fan>(device);
    }

    [Fact]
    public void Create_Thermostat_ReturnsThermostatInstance()
    {
        var device = Factory.Create("Test Thermostat", "Living Room", DeviceType.Thermostat);
        Assert.IsType<Thermostat>(device);
    }

    [Fact]
    public void Create_DoorLock_ReturnsDoorLockInstance()
    {
        var device = Factory.Create("Test Lock", "Entryway", DeviceType.DoorLock);
        Assert.IsType<DoorLock>(device);
    }

    /// <summary>
    /// Metadata tests
    /// </summary>

    [Fact]
    public void Create_SetsNameCorrectly()
    {
        var device = Factory.Create("Living Room Lamp", "Living Room", DeviceType.Light);
        Assert.Equal("Living Room Lamp", device.Name);
    }

    [Fact]
    public void Create_SetsLocationCorrectly()
    {
        var device = Factory.Create("Living Room Lamp", "Living Room", DeviceType.Light);
        Assert.Equal("Living Room", device.Location);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var device1 = Factory.Create("Light 1", "Living Room", DeviceType.Light);
        var device2 = Factory.Create("Light 2", "Living Room", DeviceType.Light);
        Assert.NotEqual(device1.Id, device2.Id);
    }

    /// <summary>
    /// Default state tests
    /// </summary>

    [Fact]
    public void Create_Light_DefaultsToOff()
    {
        var device = Factory.Create("Test Light", "Living Room", DeviceType.Light);
        Assert.False(device.IsOn());
    }

    [Fact]
    public void Create_Fan_DefaultsToOff()
    {
        var device = Factory.Create("Test Fan", "Bedroom", DeviceType.Fan);
        Assert.False(device.IsOn());
    }

    [Fact]
    public void Create_Thermostat_DefaultsToOff()
    {
        var device = Factory.Create("Test Thermostat", "Living Room", DeviceType.Thermostat);
        Assert.False(device.IsOn());
    }

    [Fact]
    public void Create_DoorLock_DefaultsToOn()
    {
        var device = Factory.Create("Test Lock", "Entryway", DeviceType.DoorLock);
        Assert.True(device.IsOn());
    }

    /// <summary>
    /// Invalid input tests
    /// </summary>

    [Fact]
    public void Create_UnsupportedType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Factory.Create("Test", "Room", (DeviceType)99));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            Factory.Create(name!, "Living Room", DeviceType.Light));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidLocation_Throws(string? location)
    {
        Assert.Throws<ArgumentException>(() =>
            Factory.Create("Test Light", location!, DeviceType.Light));
    }
}
