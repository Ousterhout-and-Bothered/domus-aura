using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Device.Thermostat;
using DomainDevice = SmartHome.Domain.Device.Device;

namespace SmartHome.Infrastructure.Persistence.Seed;

public sealed class SmartHomeDbSeeder(IDeviceRepository repository)
{

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

        // Thermostats

        // Cooling example
        // ambient > desired (triggers cooling)
        var livingRoomThermostat = new Thermostat("Living Room Thermostat", "Living Room");
        livingRoomThermostat.SetMode(ThermostatMode.Cool);
        livingRoomThermostat.SetDesiredTemperature(72);
        livingRoomThermostat.SetAmbientTemperature(78);
        livingRoomThermostat.TurnOn();

        // Heating example
        // ambient < desired (triggers heating)
        var bedroomThermostat = new Thermostat("Bedroom Thermostat", "Bedroom");
        bedroomThermostat.SetMode(ThermostatMode.Heat);
        bedroomThermostat.SetDesiredTemperature(68);
        bedroomThermostat.SetAmbientTemperature(62);
        bedroomThermostat.TurnOn();

        // Idle example
        // ambient == desired results in idle state
        var officeThermostat = new Thermostat("Office Thermostat", "Office");
        officeThermostat.SetMode(ThermostatMode.Auto);
        officeThermostat.SetDesiredTemperature(72);
        officeThermostat.SetAmbientTemperature(72);
        officeThermostat.TurnOn();

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
            livingRoomThermostat,
            bedroomThermostat,
            officeThermostat
        };

        foreach (var device in seededDevices)
            await repository.AddAsync(device, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
    }
}