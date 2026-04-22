using System.ComponentModel.DataAnnotations;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Contracts.Devices;

/// <summary>
/// Represents a request to register a new device in the system.
/// </summary>
public sealed class RegisterDeviceRequest
{
    /// <summary>
    /// The name of the device.
    /// </summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The location where the device is installed.
    /// </summary>
    [Required]
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// The type of device to be registered.
    /// </summary>
    [Required]
    public DeviceType Type { get; init; }
}