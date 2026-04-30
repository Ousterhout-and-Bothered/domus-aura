using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class DoorLockTests
{
    private static DoorLock CreateDoorLock() => new("Front Door", "Entryway");

    [Fact]
    public void Lock_UnlockedToLocked_SetsStateToLocked()
    {
        // Arrange
        var doorLock = CreateDoorLock();

        // Act
        doorLock.Lock();

        // Assert
        Assert.Equal(DoorLockState.Locked, doorLock.LockState);
    }

    [Fact]
    public void Unlock_LockedToUnlocked_SetsStateToUnlocked()
    {
        // Arrange
        var doorLock = CreateDoorLock();
        doorLock.Lock();

        // Act
        doorLock.Unlock();

        // Assert
        Assert.Equal(DoorLockState.Unlocked, doorLock.LockState);
    }

    [Fact]
    public void Lock_LockedToLocked_ThrowsInvalidDomainOperation()
    {
        // Arrange
        var doorLock = CreateDoorLock();
        doorLock.Lock();

        // Act + Assert
        Assert.Throws<InvalidDomainOperationException>(() => doorLock.Lock());
    }

    [Fact]
    public void Unlock_UnlockedToUnlocked_ThrowsInvalidDomainOperation()
    {
        // Arrange
        var doorLock = CreateDoorLock();

        // Act + Assert
        Assert.Throws<InvalidDomainOperationException>(() => doorLock.Unlock());
    }
}