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
    
    // Required for EF Core
    private DoorLock()
    {
        Type = DeviceType.DoorLock;
        LockState = DoorLockState.Locked;
    }
    
    public DoorLock(string name, string location) : base(name, location, DeviceType.DoorLock)
    {
        LockState = DoorLockState.Locked;
    }

    
    /// <summary>
    /// Locks the device.
    /// Throws <see cref="InvalidOperationException"/> if already locked.
    /// </summary>
    public void Lock()
    {
        // Reject invalid transition — state machine must not silently no-op
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
        // Reject invalid transition — state machine must not silently no-op
        if (LockState == DoorLockState.Unlocked)
            throw new InvalidOperationException("Device is already unlocked.");
        
        LockState = DoorLockState.Unlocked;
    }
    

    /// <summary>
    /// Always returns true — latch devices have no power state.
    /// </summary>
    public override bool IsOn() => true;
}