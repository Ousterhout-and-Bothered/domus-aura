using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.Contracts.Devices;

/// <summary>
/// Represents a request to update a device's editable metadata.
/// Both fields are sent on every request; the service layer detects
/// what actually changed and logs only the deltas.
/// </summary>
public sealed class UpdateDeviceRequest
{
    /// <summary>
    /// The new name for the device.
    /// </summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The new location for the device.
    /// </summary>
    [Required]
    public string Location { get; init; } = string.Empty;
}