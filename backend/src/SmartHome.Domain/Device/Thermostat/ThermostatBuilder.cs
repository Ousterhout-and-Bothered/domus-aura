using SmartHome.Domain.Device.Registration;

namespace SmartHome.Domain.Device.Thermostat;

/// <summary>
/// Constructs <see cref="Thermostat"/> instances for the <see cref="DeviceFactory"/>.
/// </summary>
public sealed class ThermostatBuilder(IThermostatStrategyProvider strategyProvider) : IDeviceBuilder
{
    public DeviceType HandledType => DeviceType.Thermostat;

    public Device Build(string name, string location) => new Thermostat(name, location, strategyProvider);
}