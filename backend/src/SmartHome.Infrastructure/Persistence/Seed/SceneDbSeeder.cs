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
    private static readonly Guid WelcomeHomeSceneId =
        Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

    private static readonly Guid AllLightsOffSceneId =
        Guid.Parse("cccccccc-3333-3333-3333-333333333333");

    private static readonly Guid SecureSceneId =
        Guid.Parse("dddddddd-4444-4444-4444-444444444444");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingScenes = await repository.GetAllAsync(cancellationToken);

        if (existingScenes.Count > 0)
            return;

        var devices = await deviceRepository.GetAllAsync(cancellationToken: cancellationToken);

        var scenes = new List<DeviceScene>
        {
            new DeviceScene(WelcomeHomeSceneId, "Welcome Home", new[]
            {
                SceneAction.ForGroup(DeviceType.DoorLock, "Entryway", "Unlock", 0),

                SceneAction.ForGroup(DeviceType.Light, "Hallway", "SetBrightness", 1, "100"),
                SceneAction.ForGroup(DeviceType.Light, "Kitchen", "SetBrightness", 2, "100"),

                SceneAction.ForGroup(DeviceType.Fan, "Living Room", "SetSpeed", 3, "Medium"),

                SceneAction.ForGroup(DeviceType.Thermostat, "Living Room", "SetDesiredTemperature", 4, "72")
            }),

            new DeviceScene(AllLightsOffSceneId, "All Lights Off", new[]
            {
                SceneAction.ForGroup(DeviceType.Light, null, "SetPower", 0, "Off")
            })
        };

        var frontDoor = devices.FirstOrDefault(d => d.Name == "Front Door");
        var porchLight = devices.FirstOrDefault(d => d.Name == "Porch Light");

        if (frontDoor is not null && porchLight is not null)
        {
            scenes.Add(new DeviceScene(SecureSceneId, "Secure", new[]
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