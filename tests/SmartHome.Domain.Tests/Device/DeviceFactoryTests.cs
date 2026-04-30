using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Domain.Tests.Device;

public class DeviceFactoryTests
{
    private static readonly IDeviceFactory Factory = new DeviceFactory(new IDeviceBuilder[]
    {
        new LightBuilder(),
        new FanBuilder(),
        new DoorLockBuilder(),
        new ThermostatBuilder(new ThermostatStrategyProvider()),
    });

    // --- Type dispatch ---

    [Theory]
    [InlineData(DeviceType.Light, typeof(Light))]
    [InlineData(DeviceType.Fan, typeof(Fan))]
    [InlineData(DeviceType.Thermostat, typeof(Thermostat))]
    [InlineData(DeviceType.DoorLock, typeof(DoorLock))]
    public void Create_ByDeviceType_ReturnsExpectedConcreteInstance(DeviceType type, Type expected)
    {
        // Act
        var device = Factory.Create("Test Device", "Living Room", type);

        // Assert
        Assert.IsType(expected, device);
    }

    // --- Metadata ---

    [Fact]
    public void Create_SetsNameCorrectly()
    {
        // Act
        var device = Factory.Create("Living Room Lamp", "Living Room", DeviceType.Light);

        // Assert
        Assert.Equal("Living Room Lamp", device.Name);
    }

    [Fact]
    public void Create_SetsLocationCorrectly()
    {
        // Act
        var device = Factory.Create("Living Room Lamp", "Living Room", DeviceType.Light);

        // Assert
        Assert.Equal("Living Room", device.Location);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        // Act
        var first = Factory.Create("Light 1", "Living Room", DeviceType.Light);
        var second = Factory.Create("Light 2", "Living Room", DeviceType.Light);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    // --- Default state ---

    [Theory]
    [InlineData(DeviceType.Light, false)]
    [InlineData(DeviceType.Fan, false)]
    [InlineData(DeviceType.Thermostat, false)]
    [InlineData(DeviceType.DoorLock, true)]
    public void Create_DefaultPowerState_MatchesExpectedForType(DeviceType type, bool expectedIsOn)
    {
        // Arrange — DoorLock is "on" by default (locked); other devices default off.

        // Act
        var device = Factory.Create("Test Device", "Living Room", type);

        // Assert
        Assert.Equal(expectedIsOn, device.IsOn());
    }

    // --- Invalid input ---

    [Fact]
    public void Create_UnsupportedType_Throws()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            Factory.Create("Test", "Room", (DeviceType)99));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidName_Throws(string? name)
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            Factory.Create(name!, "Living Room", DeviceType.Light));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidLocation_Throws(string? location)
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            Factory.Create("Test Light", location!, DeviceType.Light));
    }
}
