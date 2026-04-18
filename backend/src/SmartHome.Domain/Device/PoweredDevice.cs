namespace SmartHome.Domain.Device;

/// <summary>
/// Base class for all devices that have an explicit power state.
/// </summary>
public abstract class PoweredDevice : Device, IPowerable
{
    /// <summary>
    /// Current power state of the device.
    /// </summary>
    public PowerState PowerState { get; private set; }

    // Required for EF Core — ensures proper materialization with default state
    protected PoweredDevice()
    {
        // Default all powered devices to Off on creation
        PowerState = PowerState.Off;
    }

    protected PoweredDevice(string name, string location, DeviceType type)
        : base(name, location, type)
    {
        // Initialize device in Off state per specification
        PowerState = PowerState.Off;
    }

    public virtual void TurnOn()
    {
        // Prevent invalid transition — already on
        if (PowerState == PowerState.On)
            throw new InvalidOperationException("Device is already on.");

        // Transition to On state
        PowerState = PowerState.On;

        // Allow subclasses to apply behavior on power-on (e.g., restore state)
        OnPoweredOn();
    }

    public virtual void TurnOff()
    {
        // Prevent invalid transition (already off)
        if (PowerState == PowerState.Off)
            throw new InvalidOperationException("Device is already off.");

        // Transition to Off state
        PowerState = PowerState.Off;

        // Allow subclasses to handle shutdown behavior
        OnPoweredOff();
    }

    // Hook for subclasses
    // Executed after device is powered on
    protected virtual void OnPoweredOn() { }

    // Hook for subclasses
    // Executed after device is powered off
    protected virtual void OnPoweredOff() { }

    public override bool IsOn() => PowerState == PowerState.On;
    
    public override void ResetToDefaults()
    {
        // Reset device-specific attributes 
        ResetPoweredDefaults();

        // Ensure device is powered off after reset
        ForceOff();
    }

    // Subclasses must define how their attributes reset
    protected abstract void ResetPoweredDefaults();

    protected void ForceOff()
    {
        // Force power state to Off without triggering validation or hooks
        PowerState = PowerState.Off;
    }
}