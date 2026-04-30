namespace SmartHome.Domain.Device.DoorLock;


/// <summary>
/// Defines the contract for latch devices that support lock and unlock.
/// Latch devices have no power state and are always considered on.
/// </summary>
public interface ILockable
{

    /// <summary>
    /// The current lock state of the device.
    /// </summary>
    DoorLockState LockState { get; }


    /// <summary>
    /// Locks the device.
    /// Throws <see cref="InvalidOperationException"/> if the device is already locked.
    /// </summary>
    void Lock();


    /// <summary>
    /// Unlocks the device.
    /// Throws <see cref="InvalidOperationException"/> if the device is already unlocked.
    /// </summary>
    void Unlock();

}