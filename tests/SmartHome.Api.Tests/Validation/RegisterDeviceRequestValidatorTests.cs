using FluentValidation.TestHelper;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Api.Validation;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Tests.Validation;

public class RegisterDeviceRequestValidatorTests
{
    private readonly RegisterDeviceRequestValidator _validator = new();

    [Fact]
    public void RegisterDeviceRequest_MissingName_Fails()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = null!, Location = "Living Room", Type = DeviceType.Light };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RegisterDeviceRequest_EmptyName_Fails()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = "", Location = "Living Room", Type = DeviceType.Light };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RegisterDeviceRequest_MissingLocation_Fails()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = "Light", Location = null!, Type = DeviceType.Light };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void RegisterDeviceRequest_ValidRequest_Passes()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = "Light", Location = "Living Room", Type = DeviceType.Light };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}