using SmartHome.Domain.Device;
using DeviceBase = SmartHome.Domain.Device.Device;
using LightDevice = SmartHome.Domain.Device.Light.Light;
using FanDevice = SmartHome.Domain.Device.Fan.Fan;
using DoorLockDevice = SmartHome.Domain.Device.DoorLock.DoorLock;
using ThermostatDevice = SmartHome.Domain.Device.Thermostat.Thermostat;

namespace SmartHome.Infrastructure.Device.Events;

/// <summary>
/// Builds serializable SSE payload snapshots from device domain models.
/// </summary>
/// <remarks>
/// The payload represents a device-specific snapshot of the current state.
/// The exact fields included vary by device type.
/// </remarks>
public static class DeviceEventPayloadFactory
{
    public static object Create(DeviceBase device) =>
        device switch
        {
            LightDevice light => new
            {
                light.Id,
                light.Name,
                light.Location,
                Type = DeviceType.Light,
                light.PowerState,
                light.Brightness,
                light.ColorHex
            },

            FanDevice fan => new
            {
                fan.Id,
                fan.Name,
                fan.Location,
                Type = DeviceType.Fan,
                fan.PowerState,
                fan.Speed
            },

            ThermostatDevice thermostat => new
            {
                thermostat.Id,
                thermostat.Name,
                thermostat.Location,
                Type = DeviceType.Thermostat,
                thermostat.State,
                thermostat.Mode,
                thermostat.DesiredTemperature,
                thermostat.AmbientTemperature
            },

            DoorLockDevice doorLock => new
            {
                doorLock.Id,
                doorLock.Name,
                doorLock.Location,
                Type = DeviceType.DoorLock,
                doorLock.LockState
            },

            _ => new
            {
                device.Id,
                device.Name,
                device.Location,
                device.Type
            }
        };
}