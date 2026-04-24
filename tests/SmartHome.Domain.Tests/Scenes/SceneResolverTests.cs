using Moq;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.DoorLock;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;
using SmartHome.Infrastructure.Scene;

namespace SmartHome.Domain.Tests.Scenes;

public class SceneResolverTests
{
    // --- Test doubles ---

    private static Light MakeLight(string name = "Test Light", string location = "Living Room") =>
        new(name, location);

    private static DoorLock MakeDoorLock(string name = "Front Door", string location = "Entryway") =>
        new(name, location);

    private static IDeviceCommand MakeStubCommand(string operationName = "Lock")
    {
        var stub = new Mock<IDeviceCommand>();
        stub.SetupGet(c => c.OperationName).Returns(operationName);
        stub.Setup(c => c.Execute()).Returns(new CommandResult(operationName, Success: true));
        return stub.Object;
    }

    // --- Happy paths ---

    [Fact]
    public async Task ResolveAsync_DeviceTargetAction_ProducesOneCommandPerDevice()
    {
        // Arrange
        var doorLock = MakeDoorLock();
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetByIdAsync(doorLock.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(doorLock);

        var commandFactory = new Mock<IDeviceCommandFactory>();
        commandFactory.Setup(f => f.Create("Lock", null, doorLock))
                      .Returns(MakeStubCommand("Lock"));

        var scene = new DeviceScene("Lock Front Door",
            [SceneAction.ForDevice(doorLock.Id, "Lock", orderIndex: 0)]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert
        Assert.Single(resolved.Composite.Children);
        Assert.Single(resolved.DeviceIdsInOrder);
        Assert.Equal(doorLock.Id, resolved.DeviceIdsInOrder[0]);
    }

    [Fact]
    public async Task ResolveAsync_GroupTargetAction_ProducesOneCommandPerMatchedDevice()
    {
        // Arrange
        var lightA = MakeLight("Light A");
        var lightB = MakeLight("Light B");
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetAllTrackedAsync("Living Room", DeviceType.Light, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([lightA, lightB]);

        var commandFactory = new Mock<IDeviceCommandFactory>();
        commandFactory.Setup(f => f.Create("SetPower", "Off", It.IsAny<Domain.Device.Device>()))
            .Returns<string, object?, Domain.Device.Device>((op, _, _) => MakeStubCommand(op));

        var scene = new DeviceScene("Lights Off",
            [SceneAction.ForGroup(DeviceType.Light, "Living Room", "SetPower", orderIndex: 0, value: "Off")]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert
        Assert.Equal(2, resolved.Composite.Children.Count);
        Assert.Equal(2, resolved.DeviceIdsInOrder.Count);
        Assert.Contains(lightA.Id, resolved.DeviceIdsInOrder);
        Assert.Contains(lightB.Id, resolved.DeviceIdsInOrder);
    }

    [Fact]
    public async Task ResolveAsync_PreservesActionOrder()
    {
        // Arrange
        var doorLock = MakeDoorLock();
        var light = MakeLight();
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetByIdAsync(doorLock.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(doorLock);
        deviceRepo.Setup(r => r.GetByIdAsync(light.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(light);

        var commandFactory = new Mock<IDeviceCommandFactory>();
        commandFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<Domain.Device.Device>()))
            .Returns<string, object?, Domain.Device.Device>((op, _, _) => MakeStubCommand(op));

        var scene = new DeviceScene("Mixed",
        [
            SceneAction.ForDevice(doorLock.Id, "Lock", orderIndex: 0),
            SceneAction.ForDevice(light.Id, "SetPower", orderIndex: 1, value: "Off"),
        ]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert
        Assert.Equal(doorLock.Id, resolved.DeviceIdsInOrder[0]);
        Assert.Equal(light.Id, resolved.DeviceIdsInOrder[1]);
    }

    // --- Finding #2: resolution-time failures don't abort the batch ---

    [Fact]
    public async Task ResolveAsync_CommandConstructionThrowsDomainException_EmitsFailedCommandStub()
    {
        // Arrange — "Lock on Light" is the canonical incompatible-op example.
        var light = MakeLight();
        var doorLock = MakeDoorLock();
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetByIdAsync(light.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(light);
        deviceRepo.Setup(r => r.GetByIdAsync(doorLock.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(doorLock);

        var commandFactory = new Mock<IDeviceCommandFactory>();
        commandFactory.Setup(f => f.Create("Lock", null, light))
                      .Throws(new InvalidDomainOperationException("Command 'Lock' is not supported by 'Light'."));
        commandFactory.Setup(f => f.Create("Lock", null, doorLock))
                      .Returns(MakeStubCommand("Lock"));

        var scene = new DeviceScene("Lock Everything",
        [
            SceneAction.ForDevice(light.Id, "Lock", orderIndex: 0),
            SceneAction.ForDevice(doorLock.Id, "Lock", orderIndex: 1),
        ]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert — both actions produce a child command (sibling actions don't abort).
        Assert.Equal(2, resolved.Composite.Children.Count);
        Assert.Equal(2, resolved.DeviceIdsInOrder.Count);

        // The light's slot is a FailedCommand whose execution reports the error.
        var lightSlotResult = resolved.Composite.Children[0].Execute();
        Assert.False(lightSlotResult.Success);
        Assert.Contains("not supported", lightSlotResult.Message);

        // The light's device id is preserved in the parallel list.
        Assert.Equal(light.Id, resolved.DeviceIdsInOrder[0]);
    }

    // --- Finding #3: zero-resolution actions surface as failed entries ---

    [Fact]
    public async Task ResolveAsync_DeletedDeviceTarget_EmitsFailedCommandWithEmptyGuid()
    {
        // Arrange
        var missingDeviceId = Guid.NewGuid();
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetByIdAsync(missingDeviceId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Domain.Device.Device?)null);

        var commandFactory = new Mock<IDeviceCommandFactory>();

        var scene = new DeviceScene("Targets Deleted Device",
            [SceneAction.ForDevice(missingDeviceId, "Lock", orderIndex: 0)]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert
        Assert.Single(resolved.Composite.Children);
        Assert.Single(resolved.DeviceIdsInOrder);

        // Guid.Empty signals "no real device" so SceneService skips history logging.
        Assert.Equal(Guid.Empty, resolved.DeviceIdsInOrder[0]);

        // The original device id is preserved in the failure message for traceability.
        var result = resolved.Composite.Children[0].Execute();
        Assert.False(result.Success);
        Assert.Contains(missingDeviceId.ToString(), result.Message);
    }

    [Fact]
    public async Task ResolveAsync_EmptyGroupTarget_EmitsFailedCommandWithEmptyGuid()
    {
        // Arrange
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetAllTrackedAsync("Garage", DeviceType.Light, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var commandFactory = new Mock<IDeviceCommandFactory>();

        var scene = new DeviceScene("Garage Lights Off",
            [SceneAction.ForGroup(DeviceType.Light, "Garage", "SetPower", orderIndex: 0, value: "Off")]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert
        Assert.Single(resolved.Composite.Children);
        Assert.Single(resolved.DeviceIdsInOrder);
        Assert.Equal(Guid.Empty, resolved.DeviceIdsInOrder[0]);

        var result = resolved.Composite.Children[0].Execute();
        Assert.False(result.Success);
        Assert.Contains("Garage", result.Message);
    }

    // --- Positional contract ---

    [Fact]
    public async Task ResolveAsync_AlwaysProducesParallelCompositeAndDeviceIdLists()
    {
        // Arrange — a deliberately mixed scene exercising one happy path, one deleted
        // target, and one empty group, all in the same resolve call.
        var light = MakeLight();
        var missingDeviceId = Guid.NewGuid();
        var deviceRepo = new Mock<IDeviceRepository>();
        deviceRepo.Setup(r => r.GetByIdAsync(light.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(light);
        deviceRepo.Setup(r => r.GetByIdAsync(missingDeviceId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Domain.Device.Device?)null);
        deviceRepo.Setup(r => r.GetAllTrackedAsync("Garage", DeviceType.Light, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var commandFactory = new Mock<IDeviceCommandFactory>();
        commandFactory.Setup(f => f.Create("SetPower", "Off", light))
                      .Returns(MakeStubCommand("SetPower"));

        var scene = new DeviceScene("Mixed Failure Modes",
        [
            SceneAction.ForDevice(light.Id, "SetPower", orderIndex: 0, value: "Off"),
            SceneAction.ForDevice(missingDeviceId, "Lock", orderIndex: 1),
            SceneAction.ForGroup(DeviceType.Light, "Garage", "SetPower", orderIndex: 2, value: "Off"),
        ]);

        var resolver = new SceneResolver(deviceRepo.Object, commandFactory.Object);

        // Act
        var resolved = await resolver.ResolveAsync(scene);

        // Assert — three actions, three commands, three device-id slots, regardless of failures.
        Assert.Equal(3, resolved.Composite.Children.Count);
        Assert.Equal(3, resolved.DeviceIdsInOrder.Count);
    }
}
