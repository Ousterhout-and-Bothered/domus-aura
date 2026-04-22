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
    /// <summary>
    /// Seeds the database with a default set of devices if it is currently empty.
    /// This method is idempotent and will not duplicate data on subsequent runs.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Prevent duplicate seeding
        if (await repository.AnyAsync(cancellationToken))
            return;

        // Door Locks
        var frontDoor = new DoorLock("Front Door", "Entryway");

        var backDoor = new DoorLock("Back Door", "Patio");

        // Fans
        var livingRoomFan = new Fan("Living Room Fan", "Living Room");
        livingRoomFan.TurnOn();
        // Set non-default speed
        livingRoomFan.SetSpeed(FanSpeed.High);

        var bedroomFan = new Fan("Bedroom Fan", "Bedroom");
        bedroomFan.TurnOn();
        bedroomFan.SetSpeed(FanSpeed.Low);
        // Turn off after configuration to retain settings
        bedroomFan.TurnOff();

        // Lights
        var kitchenOverhead = new Light("Kitchen Overhead", "Kitchen");
        kitchenOverhead.TurnOn();
        // Set custom brightness and color
        kitchenOverhead.SetBrightness(75);
        kitchenOverhead.SetColor("#FF8800");

        var livingRoomOverhead = new Light("Living Room Overhead", "Living Room");
        livingRoomOverhead.TurnOn();
        livingRoomOverhead.SetBrightness(40);
        livingRoomOverhead.SetColor("#FFF4CC");

        var hallwayLight = new Light("Hallway Light", "Hallway");
        hallwayLight.TurnOn();
        hallwayLight.SetBrightness(100);
        hallwayLight.SetColor("#FFFFFF");
        // Turn off to verify state restoration on next power-on
        hallwayLight.TurnOff();

        var porchLight = new Light("Porch Light", "Entryway");
        porchLight.TurnOn();
        porchLight.SetBrightness(85);
        porchLight.SetColor("#FFC0CB");

        // Thermostats

        // Cooling example
        // ambient > desired (triggers cooling)
        var livingRoomThermostat = new Thermostat("Living Room Thermostat", "Living Room");
        livingRoomThermostat.TurnOn();
        livingRoomThermostat.SetMode(ThermostatMode.Cool);
        livingRoomThermostat.SetDesiredTemperature(72);
        livingRoomThermostat.SetAmbientTemperature(78);

        // Heating example
        // ambient < desired (triggers heating)
        var bedroomThermostat = new Thermostat("Bedroom Thermostat", "Bedroom");
        bedroomThermostat.TurnOn();
        bedroomThermostat.SetMode(ThermostatMode.Heat);
        bedroomThermostat.SetDesiredTemperature(68);
        bedroomThermostat.SetAmbientTemperature(62);

        // Idle example
        // ambient == desired results in idle state
        var officeThermostat = new Thermostat("Office Thermostat", "Office");
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
            kitchenOverhead,
            livingRoomOverhead,
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