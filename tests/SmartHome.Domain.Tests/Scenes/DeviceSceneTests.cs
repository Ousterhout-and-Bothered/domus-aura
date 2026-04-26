using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Scene;

namespace SmartHome.Domain.Tests.Scenes;

public class DeviceSceneTests
{
    private static SceneAction MakeGroupAction(string operation = "SetPower", string? value = "Off") =>
        SceneAction.ForGroup(DeviceType.Light, "Living Room", operation, orderIndex: 0, value: value);

    private static SceneAction MakeDeviceAction(Guid? deviceId = null, string operation = "Lock") =>
        SceneAction.ForDevice(deviceId ?? Guid.NewGuid(), operation, orderIndex: 0);

    [Fact]
    public void Constructor_ValidArguments_SetsNameAndAssignsId()
    {
        // Act
        var scene = new DeviceScene("Good Night", [MakeGroupAction()]);

        // Assert
        Assert.Equal("Good Night", scene.Name);
        Assert.NotEqual(Guid.Empty, scene.Id);
    }

    [Fact]
    public void Constructor_RenumbersActionsToZeroIndexedSequence()
    {
        // Arrange — each helper sets orderIndex: 0; the aggregate must overwrite to 0..n-1.
        var actions = new[]
        {
            MakeGroupAction("SetPower"),
            MakeDeviceAction(operation: "Lock"),
            MakeGroupAction("SetBrightness", "50"),
        };

        // Act
        var scene = new DeviceScene("Mixed", actions);

        // Assert
        Assert.Equal(0, scene.Actions[0].OrderIndex);
        Assert.Equal(1, scene.Actions[1].OrderIndex);
        Assert.Equal(2, scene.Actions[2].OrderIndex);
    }

    [Fact]
    public void Constructor_NullName_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            new DeviceScene(null!, [MakeGroupAction()]));
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            new DeviceScene("   ", [MakeGroupAction()]));
    }

    [Fact]
    public void Constructor_EmptyActionList_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            new DeviceScene("Good Night", Array.Empty<SceneAction>()));
    }

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        // Arrange
        var scene = new DeviceScene("Good Night", [MakeGroupAction()]);

        // Act
        scene.Rename("Bedtime");

        // Assert
        Assert.Equal("Bedtime", scene.Name);
    }

    [Fact]
    public void Rename_WhitespaceName_ThrowsInvalidDomainArgument()
    {
        // Arrange
        var scene = new DeviceScene("Good Night", [MakeGroupAction()]);

        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() => scene.Rename("   "));
    }

    [Fact]
    public void ReplaceActions_NewCollection_DiscardsOldAndRenumbersNew()
    {
        // Arrange
        var scene = new DeviceScene("Good Night", [MakeGroupAction(), MakeGroupAction(), MakeGroupAction()]);
        var originalIds = scene.Actions.Select(a => a.Id).ToList();

        var replacements = new[]
        {
            MakeDeviceAction(operation: "Lock"),
            MakeDeviceAction(operation: "Unlock"),
        };

        // Act
        scene.ReplaceActions(replacements);

        // Assert
        Assert.Equal(2, scene.Actions.Count);
        Assert.Equal(0, scene.Actions[0].OrderIndex);
        Assert.Equal(1, scene.Actions[1].OrderIndex);
        // None of the original action ids should remain.
        Assert.Empty(scene.Actions.Select(a => a.Id).Intersect(originalIds));
    }

    [Fact]
    public void ReplaceActions_EmptyCollection_ThrowsInvalidDomainArgument()
    {
        // Arrange
        var scene = new DeviceScene("Good Night", [MakeGroupAction()]);

        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            scene.ReplaceActions(Array.Empty<SceneAction>()));
    }

    [Fact]
    public void Actions_ReturnsSortedByOrderIndex()
    {
        // Arrange — after construction the OrderIndex sequence is 0..n-1, so the property's
        // sort is observable as "matches construction order." This pins the contract:
        // consumers see actions in OrderIndex order regardless of internal state.
        var actions = new[]
        {
            MakeGroupAction("SetPower"),
            MakeDeviceAction(operation: "Lock"),
            MakeGroupAction("SetBrightness", "50"),
        };

        // Act
        var scene = new DeviceScene("Mixed", actions);

        // Assert
        var orderIndices = scene.Actions.Select(a => a.OrderIndex).ToList();
        Assert.Equal(new[] { 0, 1, 2 }, orderIndices);
    }
    
    [Fact]
    public void Constructor_NullActions_ThrowsInvalidDomainArgument()
    {
        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() =>
            new DeviceScene("Good Night", actions: null!));
    }

    [Fact]
    public void ReplaceActions_NullCollection_ThrowsInvalidDomainArgument()
    {
        // Arrange
        var scene = new DeviceScene("Good Night", [MakeGroupAction()]);

        // Act + Assert
        Assert.Throws<InvalidDomainArgumentException>(() => scene.ReplaceActions(null!));
    }
}
