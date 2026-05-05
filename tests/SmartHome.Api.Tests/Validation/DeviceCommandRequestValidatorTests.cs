using FluentValidation.TestHelper;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Api.Validation;

namespace SmartHome.Api.Tests.Validation;

public class DeviceCommandRequestValidatorTests
{
    private readonly DeviceCommandRequestValidator _validator = new();

    [Fact]
    public void DeviceCommandRequest_MissingCommand_Fails()
    {
        // Arrange
        var request = new DeviceCommandRequest(null!, "on");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Command);
    }

    // --- SetBrightness ---

    [Theory]
    [InlineData("setBrightness", 5)]
    [InlineData("setBrightness", 101)]
    [InlineData("setBrightness", "high")]
    [InlineData("setBrightness", null)]
    public void SetBrightness_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setBrightness", 10)]
    [InlineData("setBrightness", 50)]
    [InlineData("setBrightness", 100)]
    public void SetBrightness_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(9, "Brightness must be between 10 and 100.")]
    [InlineData(101, "Brightness must be between 10 and 100.")]
    [InlineData("not-a-number", "Brightness must be a valid integer.")]
    public void SetBrightness_SpecificErrors(object value, string expectedError)
    {
        // Arrange
        var request = new DeviceCommandRequest("setBrightness", value);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage(expectedError);
    }

    // --- SetPower ---

    [Theory]
    [InlineData("setPower", "invalid")]
    [InlineData("setPower", null)]
    public void SetPower_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setPower", "On")]
    [InlineData("setPower", "Off")]
    public void SetPower_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(59, "Temperature must be between 60 and 80.")]
    [InlineData(81, "Temperature must be between 60 and 80.")]
    public void SetDesiredTemperature_SpecificErrors(object value, string expectedError)
    {
        // Arrange
        var request = new DeviceCommandRequest("setDesiredTemperature", value);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage(expectedError);
    }

    // --- SetSpeed ---

    [Theory]
    [InlineData("setSpeed", "invalid")]
    [InlineData("setSpeed", null)]
    public void SetSpeed_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setSpeed", "Low")]
    [InlineData("setSpeed", "Medium")]
    [InlineData("setSpeed", "High")]
    public void SetSpeed_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // --- SetMode ---

    [Fact]
    public void SetSpeed_InvalidFanSpeed_FailsWithSpecificMessage()
    {
        // Arrange
        var request = new DeviceCommandRequest("setSpeed", "UltraFast");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Fan speed must be one of: Low, Medium, High.");
    }

    [Theory]
    [InlineData("setMode", "invalid")]
    [InlineData("setMode", null)]
    public void SetMode_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setMode", "Heat")]
    [InlineData("setMode", "Cool")]
    [InlineData("setMode", "Auto")]
    public void SetMode_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // --- SetDesiredTemperature ---

    [Theory]
    [InlineData("setDesiredTemperature", 59)]
    [InlineData("setDesiredTemperature", 81)]
    [InlineData("setDesiredTemperature", "hot")]
    [InlineData("setDesiredTemperature", null)]
    public void SetDesiredTemperature_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setDesiredTemperature", 60)]
    [InlineData("setDesiredTemperature", 70)]
    [InlineData("setDesiredTemperature", 80)]
    public void SetDesiredTemperature_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // --- SetColor ---

    [Theory]
    [InlineData("setColor", "red")]
    [InlineData("setColor", "#FF000")]
    [InlineData("setColor", "FF0000")]
    [InlineData("setColor", null)]
    public void SetColor_InvalidValues_Fail(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setColor", "#FF0000")]
    [InlineData("setColor", "#aabbcc")]
    public void SetColor_ValidValues_Pass(string command, object? value)
    {
        // Arrange
        var request = new DeviceCommandRequest(command, value);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }
}
