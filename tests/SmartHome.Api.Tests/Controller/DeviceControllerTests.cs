using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Api.Controller;
using SmartHome.Domain.Device;
using SmartHome.Infrastructure.Device.Events;
using Xunit;

namespace SmartHome.Api.Tests.Controller;

public class DeviceControllerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly Mock<IDeviceEventStream> _deviceEventStreamMock;
    private readonly Mock<IOptions<JsonOptions>> _jsonOptionsMock;
    private readonly DeviceController _controller;

    public DeviceControllerTests()
    {
        _deviceServiceMock = new Mock<IDeviceService>();
        _deviceEventStreamMock = new Mock<IDeviceEventStream>();
        _jsonOptionsMock = new Mock<IOptions<JsonOptions>>();
        _jsonOptionsMock.Setup(o => o.Value).Returns(new JsonOptions());

        _controller = new DeviceController(
            _deviceServiceMock.Object,
            _deviceEventStreamMock.Object,
            _jsonOptionsMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllDevices()
    {
        // Arrange
        var devices = new List<Device>
        {
            new SmartHome.Domain.Device.Light.Light(Guid.NewGuid(), "Light", "Living Room")
        };
        _deviceServiceMock.Setup(s => s.GetAllDevicesAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        // Act
        var result = await _controller.GetAll(null, null, null, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<Device>>(okResult.Value);
        Assert.Single(response);
    }

    [Fact]
    public async Task GetById_ReturnsDevice()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new SmartHome.Domain.Device.Light.Light(deviceId, "Light", "Living Room");
        _deviceServiceMock.Setup(s => s.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _controller.GetById(deviceId, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartHome.Domain.Device.Light.Light>(okResult.Value);
        Assert.Equal(deviceId, response.Id);
    }

    [Fact]
    public async Task Create_ReturnsCreatedDevice()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = "Light", Location = "Living Room", Type = DeviceType.Light };
        var device = new SmartHome.Domain.Device.Light.Light(Guid.NewGuid(), "Light", "Living Room");
        _deviceServiceMock.Setup(s => s.RegisterDeviceAsync(request.Name, request.Location, request.Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _controller.Create(request, default);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SmartHome.Domain.Device.Light.Light>(createdResult.Value);
        Assert.Equal("Light", response.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        // Act
        var result = await _controller.Delete(deviceId, default);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _deviceServiceMock.Verify(s => s.RemoveDeviceAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistory_ReturnsHistory()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var history = new List<CommandHistory>
        {
            new CommandHistory(deviceId, "TurnOn")
        };
        _deviceServiceMock.Setup(s => s.GetDeviceHistoryAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.GetHistory(deviceId, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<CommandHistory>>(okResult.Value);
        Assert.Single(response);
    }

    [Fact]
    public async Task UpdateState_ReturnsUpdatedDevice()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var request = new DeviceCommandRequest("TurnOn", "on");
        var device = new SmartHome.Domain.Device.Light.Light(deviceId, "Light", "Living Room");
        _deviceServiceMock.Setup(s => s.ExecuteCommandAsync(deviceId, request.Command, "on", It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _controller.UpdateState(deviceId, request, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartHome.Domain.Device.Light.Light>(okResult.Value);
        Assert.Equal(deviceId, response.Id);
    }
}
