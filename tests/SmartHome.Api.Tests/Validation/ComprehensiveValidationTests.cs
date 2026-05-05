using FluentValidation.TestHelper;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Api.Validation;
using SmartHome.Domain.Device;
using Xunit;

namespace SmartHome.Api.Tests.Validation;

public class ComprehensiveValidationTests
{
    private readonly DeviceCommandRequestValidator _commandValidator = new();
    private readonly RegisterDeviceRequestValidator _registerValidator = new();

    // --- Missing Required Fields ---

    [Fact]
    public void RegisterDeviceRequest_MissingName_Fails()
    {
        // Arrange
        var request = new RegisterDeviceRequest { Name = null!, Location = "Living Room", Type = DeviceType.Light };

        // Act
        var result = _registerValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void DeviceCommandRequest_MissingCommand_Fails()
    {
        // Arrange
        var request = new DeviceCommandRequest(null!, "on");

        // Act
        var result = _commandValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Command);
    }

    // --- Invalid Data Types ---
    // Note: In C#, invalid data types for JSON are often handled by the serializer before validation,
    // but we can test validator logic for string-based inputs that should be numeric.

    [Fact]
    public void DeviceCommandRequest_InvalidBrightnessType_Fails()
    {
        // Arrange
        var request = new DeviceCommandRequest("setBrightness", "not-a-number");

        // Act
        var result = _commandValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Brightness must be a valid integer.");
    }

    // --- Out-of-Range Values ---

    [Theory]
    [InlineData(9)]
    [InlineData(101)]
    public void DeviceCommandRequest_BrightnessOutOfRange_Fails(int value)
    {
        // Arrange
        var request = new DeviceCommandRequest("setBrightness", value.ToString());

        // Act
        var result = _commandValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Brightness must be between 10 and 100.");
    }

    [Theory]
    [InlineData(59)]
    [InlineData(81)]
    public void DeviceCommandRequest_TemperatureOutOfRange_Fails(int value)
    {
        // Arrange
        var request = new DeviceCommandRequest("setDesiredTemperature", value.ToString());

        // Act
        var result = _commandValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Temperature must be between 60 and 80.");
    }

    [Fact]
    public void DeviceCommandRequest_InvalidFanSpeed_Fails()
    {
        // Arrange
        var request = new DeviceCommandRequest("setSpeed", "UltraFast");

        // Act
        var result = _commandValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Fan speed must be one of: Low, Medium, High.");
    }
}
