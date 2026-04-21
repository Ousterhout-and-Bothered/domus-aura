using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Tests.Device;

public class DoorLockTests
{
    /// <summary>
    /// Helpers
    /// </summary>

    // A freshly-constructed DoorLock is Unlocked per spec §1.1.4 / §1.4 / §3.5.
    private static DoorLock CreateUnlocked() => new("Front Door", "Entryway");

    // Drive a fresh DoorLock to the Locked state for tests that need it.
    private static DoorLock CreateLocked()
    {
        var doorLock = CreateUnlocked();
        doorLock.Lock();
        return doorLock;
    }

    /// <summary>
    /// Initial state test
    /// </summary>

    [Fact]
    public void DoorLock_DefaultState_IsUnlocked()
    {
        // Arrange & Act
        var doorLock = new DoorLock("Front Door", "Entryway");

        // Assert — spec §1.1.4 state diagram: [*] --> Unlocked
        Assert.Equal(DoorLockState.Unlocked, doorLock.LockState);
    }

    [Fact]
    public void DoorLock_IsAlwaysOn()
    {
        // Arrange & Act
        var doorLock = CreateUnlocked();

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

    /// <summary>
    /// Reset
    /// </summary>

    [Fact]
    public void ResetToDefaults_WhenLocked_ReturnsToUnlocked()
    {
        // Arrange — spec §1.4: factory default for door locks is Unlocked
        var doorLock = CreateLocked();

        // Act
        doorLock.ResetToDefaults();

        // Assert
        Assert.Equal(DoorLockState.Unlocked, doorLock.LockState);
    }
}