using SmartHome.Domain.Device;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class DoorLockTests
{
    private static DoorLock CreateDoorLock() => new("Front Door", "Entryway");

    [Fact]
    public void Lock_UnlockedToLocked_SetsStateToLocked()
    {
        var doorLock = CreateDoorLock();
        doorLock.Lock();
        Assert.Equal(DoorLockState.Locked, doorLock.LockState);
    }

    [Fact]
    public void Unlock_LockedToUnlocked_SetsStateToUnlocked()
    {
        var doorLock = CreateDoorLock();
        doorLock.Lock();
        doorLock.Unlock();
        Assert.Equal(DoorLockState.Unlocked, doorLock.LockState);
    }

    [Fact]
    public void Lock_LockedToLocked_ThrowsInvalidOperationException()
    {
        var doorLock = CreateDoorLock();
        doorLock.Lock();
        Assert.Throws<InvalidDomainOperationException>(() => doorLock.Lock());
    }

    [Fact]
    public void Unlock_UnlockedToUnlocked_ThrowsInvalidOperationException()
    {
        var doorLock = CreateDoorLock();
        Assert.Throws<InvalidDomainOperationException>(() => doorLock.Unlock());
    }
}