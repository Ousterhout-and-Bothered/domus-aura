using SmartHome.Domain.Enum;

namespace SmartHome.Domain.Device;

public sealed class DoorLock : Device
{
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

    public void Lock()
    {
        LockState = DoorLockState.Locked;
    }

    public void Unlock()
    {
        LockState = DoorLockState.Unlocked;
    }

    public override bool IsOn() => true;
}