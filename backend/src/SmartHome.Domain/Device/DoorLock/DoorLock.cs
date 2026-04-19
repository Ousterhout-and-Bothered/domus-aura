namespace SmartHome.Domain.Device.DoorLock;

/// <summary>
/// Represents a latch device that can be locked or unlocked.
/// DoorLocks have no power state and are always considered on.
/// </summary>
public sealed class DoorLock : Device, ILockable
{
    /// <summary>
    /// The current lock state of the device.
    /// </summary>
    public DoorLockState LockState { get; private set; }

    private DoorLock()
    {
        Type = DeviceType.DoorLock;
        LockState = DoorLockState.Unlocked;
    }

    public DoorLock(string name, string location) : base(name, location, DeviceType.DoorLock)
    {
        LockState = DoorLockState.Unlocked;
    }

    /// <summary>
    /// Locks the device.
    /// Throws <see cref="InvalidOperationException"/> if already locked.
    /// </summary>
    public void Lock()
    {
        if (LockState == DoorLockState.Locked)
            throw new InvalidOperationException("Device is already locked.");

        LockState = DoorLockState.Locked;
    }

    /// <summary>
    /// Unlocks the device.
    /// Throws <see cref="InvalidOperationException"/> if already unlocked.
    /// </summary>
    public void Unlock()
    {
        if (LockState == DoorLockState.Unlocked)
            throw new InvalidOperationException("Device is already unlocked.");

        LockState = DoorLockState.Unlocked;
    }

    /// <summary>
    /// Always returns true — latch devices have no power state.
    /// </summary>
    public override bool IsOn() => true;
    
    /// <summary>
    /// Resets the door lock to its default state.
    /// </summary>
    public override void ResetToDefaults()
    {
        // Door locks start in the unlocked state
        LockState = DoorLockState.Unlocked; 
    }

    /// <summary>
    /// Log-friendly representation including lock state.
    /// </summary>
    public override string ToString() =>
        $"{nameof(DoorLock)}(Id={Id}, Name='{Name}', Location='{Location}', LockState={LockState})";
}
