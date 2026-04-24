using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Scene;

namespace SmartHome.Domain.Tests.Scenes;

public class SceneActionTests
{
    [Fact]
    public void ForDevice_ValidArguments_SetsAllProperties()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var action = SceneAction.ForDevice(deviceId, "SetPower", orderIndex: 0, value: "On");

        // Assert
        Assert.Equal(deviceId, action.DeviceId);
        Assert.Null(action.DeviceType);
        Assert.Null(action.Location);
        Assert.Equal("SetPower", action.Operation);
        Assert.Equal("On", action.Value);
        Assert.Equal(0, action.OrderIndex);
        Assert.True(action.TargetsDevice);
        Assert.False(action.TargetsGroup);
    }

    [Fact]
    public void ForGroup_ValidArguments_SetsAllProperties()
    {
        // Act
        var action = SceneAction.ForGroup(DeviceType.Light, "Living Room", "SetPower", orderIndex: 0, value: "Off");

        // Assert
        Assert.Null(action.DeviceId);
        Assert.Equal(DeviceType.Light, action.DeviceType);
        Assert.Equal("Living Room", action.Location);
        Assert.Equal("SetPower", action.Operation);
        Assert.Equal("Off", action.Value);
        Assert.True(action.TargetsGroup);
        Assert.False(action.TargetsDevice);
    }

    [Fact]
    public void ForGroup_NullLocation_AllowedAsAnyLocation()
    {
        // Act
        var action = SceneAction.ForGroup(DeviceType.Light, location: null, "SetPower", orderIndex: 0);

        // Assert
        Assert.Null(action.Location);
        Assert.True(action.TargetsGroup);
    }

    [Fact]
    public void ForDevice_EmptyOperation_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            SceneAction.ForDevice(Guid.NewGuid(), operation: "", orderIndex: 0));
    }

    [Fact]
    public void ForDevice_WhitespaceOperation_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            SceneAction.ForDevice(Guid.NewGuid(), operation: "   ", orderIndex: 0));
    }

    [Fact]
    public void ForGroup_EmptyOperation_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            SceneAction.ForGroup(DeviceType.Light, "Living Room", operation: "", orderIndex: 0));
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        // Arrange + Act
        var first = SceneAction.ForDevice(Guid.NewGuid(), "SetPower", 0);
        var second = SceneAction.ForDevice(Guid.NewGuid(), "SetPower", 0);

        // Assert — smoke test only; Guid.NewGuid() collision is essentially impossible.
        Assert.NotEqual(first.Id, second.Id);
    }
}

