using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;

namespace SmartHome.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds default scenes for development and demo usage.
/// </summary>
public sealed class SceneDbSeeder(
    ISceneRepository repository,
    IDeviceRepository deviceRepository)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingScenes = await repository.GetAllAsync(cancellationToken);

        if (existingScenes.Count > 0)
            return;

        // Get devices so we can reference specific IDs when available.
        var devices = await deviceRepository.GetAllAsync(cancellationToken: cancellationToken);

        var scenes = new List<DeviceScene>
        {
            new DeviceScene("Good Night", new[]
            {
                SceneAction.ForGroup(DeviceType.DoorLock, null, "Lock", 0),
                SceneAction.ForGroup(DeviceType.Light, null, "SetPower", 1, "Off"),
                SceneAction.ForGroup(DeviceType.Fan, null, "SetPower", 2, "Off"),
                SceneAction.ForGroup(DeviceType.Thermostat, "Bedroom", "SetDesiredTemperature", 3, "68")
            }),

            new DeviceScene("Movie Night", new[]
            {
                SceneAction.ForGroup(DeviceType.Light, "Living Room", "SetBrightness", 0, "20"),
                SceneAction.ForGroup(DeviceType.Light, "Kitchen", "SetPower", 1, "Off"),
                SceneAction.ForGroup(DeviceType.Fan, "Living Room", "SetPower", 2, "On"),
                SceneAction.ForGroup(DeviceType.Fan, "Living Room", "SetSpeed", 3, "Low"),
                SceneAction.ForGroup(DeviceType.DoorLock, "Entryway", "Lock", 4)
            }),

            new DeviceScene("Welcome Home", new[]
            {
                SceneAction.ForGroup(DeviceType.DoorLock, "Entryway", "Unlock", 0),

                SceneAction.ForGroup(DeviceType.Light, "Hallway", "SetPower", 1, "On"),
                SceneAction.ForGroup(DeviceType.Light, "Kitchen", "SetPower", 2, "On"),
                SceneAction.ForGroup(DeviceType.Light, "Hallway", "SetBrightness", 3, "100"),
                SceneAction.ForGroup(DeviceType.Light, "Kitchen", "SetBrightness", 4, "100"),

                SceneAction.ForGroup(DeviceType.Fan, "Living Room", "SetPower", 5, "On"),
                SceneAction.ForGroup(DeviceType.Fan, "Living Room", "SetSpeed", 6, "Medium"),

                SceneAction.ForGroup(DeviceType.Thermostat, "Living Room", "SetDesiredTemperature", 7, "72")
            }),

            new DeviceScene("All Lights Off", new[]
            {
                SceneAction.ForGroup(DeviceType.Light, null, "SetPower", 0, "Off")
            })
        };

        var frontDoor = devices.FirstOrDefault(d => d.Name == "Front Door");
        var porchLight = devices.FirstOrDefault(d => d.Name == "Porch Light");

        if (frontDoor is not null && porchLight is not null)
        {
            scenes.Add(new DeviceScene("Secure", new[]
            {
                SceneAction.ForDevice(frontDoor.Id, "Lock", 0),
                SceneAction.ForDevice(porchLight.Id, "SetPower", 1, "Off")
            }));
        }

        foreach (var scene in scenes)
            await repository.AddAsync(scene, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
    }
}