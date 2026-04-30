using Moq;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Registration;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class RegistrationTests
{
    private readonly Mock<IDeviceBuilder> _lightBuilderMock;
    private readonly Mock<IDeviceBuilder> _fanBuilderMock;
    private readonly Mock<IDeviceBuilder> _thermostatBuilderMock;
    private readonly Mock<IDeviceBuilder> _doorLockBuilderMock;
    private readonly DeviceFactory _factory;

    public RegistrationTests()
    {
        _lightBuilderMock = new Mock<IDeviceBuilder>();
        _lightBuilderMock.Setup(b => b.HandledType).Returns(DeviceType.Light);
        _lightBuilderMock.Setup(b => b.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string n, string l) => new Light(n, l));

        _fanBuilderMock = new Mock<IDeviceBuilder>();
        _fanBuilderMock.Setup(b => b.HandledType).Returns(DeviceType.Fan);
        _fanBuilderMock.Setup(b => b.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string n, string l) => new Fan(n, l));

        _thermostatBuilderMock = new Mock<IDeviceBuilder>();
        _thermostatBuilderMock.Setup(b => b.HandledType).Returns(DeviceType.Thermostat);
        _thermostatBuilderMock.Setup(b => b.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string n, string l) => new Thermostat(n, l, new Mock<IThermostatStrategyProvider>().Object));

        _doorLockBuilderMock = new Mock<IDeviceBuilder>();
        _doorLockBuilderMock.Setup(b => b.HandledType).Returns(DeviceType.DoorLock);
        _doorLockBuilderMock.Setup(b => b.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string n, string l) => new DoorLock(n, l));

        _factory = new DeviceFactory(new[]
        {
            _lightBuilderMock.Object,
            _fanBuilderMock.Object,
            _thermostatBuilderMock.Object,
            _doorLockBuilderMock.Object
        });
    }

    [Fact]
    public void CreateDevice_LightType_ReturnsLightInstance()
    {
        var device = _factory.Create("Light", "Room", DeviceType.Light);
        Assert.IsType<Light>(device);
    }

    [Fact]
    public void CreateDevice_FanType_ReturnsFanInstance()
    {
        var device = _factory.Create("Fan", "Room", DeviceType.Fan);
        Assert.IsType<Fan>(device);
    }

    [Fact]
    public void CreateDevice_ThermostatType_ReturnsThermostatInstance()
    {
        var device = _factory.Create("Thermostat", "Room", DeviceType.Thermostat);
        Assert.IsType<Thermostat>(device);
    }

    [Fact]
    public void CreateDevice_DoorLockType_ReturnsDoorLockInstance()
    {
        var device = _factory.Create("DoorLock", "Room", DeviceType.DoorLock);
        Assert.IsType<DoorLock>(device);
    }

    [Fact]
    public void RegisterDevice_ValidRequest_InitializesWithDefaultState()
    {
        var device = _factory.Create("New Light", "Hallway", DeviceType.Light);

        Assert.Equal("New Light", device.Name);
        Assert.Equal("Hallway", device.Location);
        Assert.Equal(PowerState.Off, ((Light)device).PowerState);
    }
}
