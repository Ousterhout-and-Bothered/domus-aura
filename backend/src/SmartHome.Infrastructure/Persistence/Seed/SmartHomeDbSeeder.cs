using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device.Thermostat;
using DomainDevice = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Persistence.Seed;

/// <summary>
/// Responsible for populating the database with initial smart home data.
/// Ensures a consistent starting state for development and testing.
/// </summary>
/// <param name="repository">The repository used to persist seeded devices.</param>
public sealed class SmartHomeDbSeeder(IDeviceRepository repository)
{
    /// <summary>Predefined identifier for the front door lock.</summary>
    private static readonly Guid FrontDoorId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Predefined identifier for the back door lock.</summary>
    private static readonly Guid BackDoorId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Predefined identifier for the living room fan.</summary>
    private static readonly Guid LivingRoomFanId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>Predefined identifier for the bedroom fan.</summary>
    private static readonly Guid BedroomFanId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary>Predefined identifier for the kitchen overhead light.</summary>
    private static readonly Guid KitchenLightId =
        Guid.Parse("55555555-5555-5555-5555-555555555555");

    /// <summary>Predefined identifier for the living room overhead light.</summary>
    private static readonly Guid LivingRoomLightId =
        Guid.Parse("66666666-6666-6666-6666-666666666666");

    /// <summary>Predefined identifier for the hallway light.</summary>
    private static readonly Guid HallwayLightId =
        Guid.Parse("77777777-7777-7777-7777-777777777777");

    /// <summary>Predefined identifier for the porch light.</summary>
    private static readonly Guid PorchLightId =
        Guid.Parse("88888888-8888-8888-8888-888888888888");

    /// <summary>Predefined identifier for the living room thermostat.</summary>
    private static readonly Guid LivingRoomThermostatId =
        Guid.Parse("99999999-9999-9999-9999-999999999999");

    /// <summary>Predefined identifier for the bedroom thermostat.</summary>
    private static readonly Guid BedroomThermostatId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>Predefined identifier for the office thermostat.</summary>
    private static readonly Guid OfficeThermostatId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    /// <summary>
    /// Seeds the database with a default set of devices if it is currently empty.
    /// This method is idempotent and will not duplicate data on subsequent runs.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Prevent duplicate seeding
        if (await repository.AnyAsync(cancellationToken))
            return;

        // Door Locks
        var frontDoor = new DoorLock(FrontDoorId, "Front Door", "Entryway");

        var backDoor = new DoorLock(BackDoorId, "Back Door", "Patio");

        // Fans
        var livingRoomFan = new Fan(LivingRoomFanId, "Living Room Fan", "Living Room");
        livingRoomFan.TurnOn();
        // Set non-default speed
        livingRoomFan.SetSpeed(FanSpeed.High);

        var bedroomFan = new Fan(BedroomFanId, "Bedroom Fan", "Bedroom");
        bedroomFan.TurnOn();
        bedroomFan.SetSpeed(FanSpeed.Low);
        // Turn off after configuration to retain settings
        bedroomFan.TurnOff();

        // Lights
        var kitchenLight = new Light(KitchenLightId, "Kitchen Overhead", "Kitchen");
        kitchenLight.TurnOn();
        // Set custom brightness and color
        kitchenLight.SetBrightness(75);
        kitchenLight.SetColor("#FF8800");

        var livingRoomLight = new Light(LivingRoomLightId, "Living Room Overhead", "Living Room");
        livingRoomLight.TurnOn();
        livingRoomLight.SetBrightness(40);
        livingRoomLight.SetColor("#FFF4CC");

        var hallwayLight = new Light(HallwayLightId, "Hallway Light", "Hallway");
        hallwayLight.TurnOn();
        hallwayLight.SetBrightness(100);
        hallwayLight.SetColor("#FFFFFF");
        // Turn off to verify state restoration on next power-on
        hallwayLight.TurnOff();

        var porchLight = new Light(PorchLightId, "Porch Light", "Entryway");
        porchLight.TurnOn();
        porchLight.SetBrightness(85);
        porchLight.SetColor("#FFC0CB");

        // Thermostats

        // Cooling example
        // ambient > desired (triggers cooling)
        var livingRoomThermostat = new Thermostat(LivingRoomThermostatId, "Living Room Thermostat", "Living Room");
        livingRoomThermostat.TurnOn();
        livingRoomThermostat.SetMode(ThermostatMode.Cool);
        livingRoomThermostat.SetDesiredTemperature(72);
        livingRoomThermostat.SetAmbientTemperature(78);

        // Heating example
        // ambient < desired (triggers heating)
        var bedroomThermostat = new Thermostat(BedroomThermostatId, "Bedroom Thermostat", "Bedroom");
        bedroomThermostat.TurnOn();
        bedroomThermostat.SetMode(ThermostatMode.Heat);
        bedroomThermostat.SetDesiredTemperature(68);
        bedroomThermostat.SetAmbientTemperature(62);

        // Idle example
        // ambient == desired results in idle state
        var officeThermostat = new Thermostat(OfficeThermostatId, "Office Thermostat", "Office");
        officeThermostat.TurnOn();
        officeThermostat.SetMode(ThermostatMode.Auto);
        officeThermostat.SetDesiredTemperature(72);
        officeThermostat.SetAmbientTemperature(72);

        // Aggregate all seeded devices into a single collection
        var seededDevices = new DomainDevice[]
        {
            frontDoor,
            backDoor,
            livingRoomFan,
            bedroomFan,
            kitchenLight,
            livingRoomLight,
            hallwayLight,
            porchLight,
            livingRoomThermostat,
            bedroomThermostat,
            officeThermostat
        };

        foreach (var device in seededDevices)
            await repository.AddAsync(device, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
    }
}