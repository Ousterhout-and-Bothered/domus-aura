using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class DoorLockTests
{
   
    /// <summary>
    /// Helpers
    /// </summary>
    
    private static DoorLock CreateLocked()
    {
        var doorLock = new DoorLock("Front Door", "Entryway");
        return doorLock;
    }

    private static DoorLock CreateUnlocked()
    {
        var doorLock = CreateLocked();
        doorLock.Unlock();
        return doorLock;
    }

    /// <summary>
    /// Initial state test
    /// </summary>

    [Fact]
    public void DoorLock_DefaultState_IsLocked()
    {
        // Arrange & Act
        var doorLock = CreateLocked();

        // Assert
        Assert.Equal(DoorLockState.Locked, doorLock.LockState);
    }

    [Fact]
    public void DoorLock_IsAlwaysOn()
    {
        // Arrange & Act
        var doorLock = CreateLocked();

        // Assert
        Assert.True(doorLock.IsOn());
    }

    /// <summary>
    /// Lock tests
    /// </summary>

    [Fact]
    public void Lock_WhenUnlocked_SetsStateToLocked()
    {
        // Arrange
        var doorLock = CreateUnlocked();

        // Act
        doorLock.Lock();

        // Assert
        Assert.Equal(DoorLockState.Locked, doorLock.LockState);
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_Throws()
    {
        // Arrange
        var doorLock = CreateLocked();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => doorLock.Lock());
    }

    /// <summary>
    /// Unlock test
    /// </summary>

    [Fact]
    public void Unlock_WhenLocked_SetsStateToUnlocked()
    {
        // Arrange
        var doorLock = CreateLocked();

        // Act
        doorLock.Unlock();

        // Assert
        Assert.Equal(DoorLockState.Unlocked, doorLock.LockState);
    }

    [Fact]
    public void Unlock_WhenAlreadyUnlocked_Throws()
    {
        // Arrange
        var doorLock = CreateUnlocked();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => doorLock.Unlock());
    }

    /// <summary>
    /// Is On
    /// </summary>

    [Fact]
    public void IsOn_WhenLocked_ReturnsTrue()
    {
        // Arrange
        var doorLock = CreateLocked();

        // Act
        var result = doorLock.IsOn();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOn_WhenUnlocked_ReturnsTrue()
    {
        // Arrange
        var doorLock = CreateUnlocked();

        // Act
        var result = doorLock.IsOn();

        // Assert
        Assert.True(result);
    }
}