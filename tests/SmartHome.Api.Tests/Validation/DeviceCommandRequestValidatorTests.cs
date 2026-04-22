using SmartHome.Api.Contracts.Devices;
using SmartHome.Api.Validation;
using Xunit;

namespace SmartHome.Api.Tests.Validation;

public class DeviceCommandRequestValidatorTests
{
    private readonly DeviceCommandRequestValidator _validator = new();

    [Theory]
    [InlineData("setBrightness", 5)]
    [InlineData("setBrightness", 101)]
    [InlineData("setBrightness", "high")]
    [InlineData("setBrightness", null)]
    public void SetBrightness_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setBrightness", 10)]
    [InlineData("setBrightness", 50)]
    [InlineData("setBrightness", 100)]
    public void SetBrightness_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setPower", "invalid")]
    [InlineData("setPower", null)]
    public void SetPower_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setPower", "On")]
    [InlineData("setPower", "Off")]
    public void SetPower_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setSpeed", "invalid")]
    [InlineData("setSpeed", null)]
    public void SetSpeed_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setSpeed", "Low")]
    [InlineData("setSpeed", "Medium")]
    [InlineData("setSpeed", "High")]
    public void SetSpeed_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setMode", "invalid")]
    [InlineData("setMode", null)]
    public void SetMode_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setMode", "Heat")]
    [InlineData("setMode", "Cool")]
    [InlineData("setMode", "Auto")]
    public void SetMode_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setDesiredTemperature", 59)]
    [InlineData("setDesiredTemperature", 81)]
    [InlineData("setDesiredTemperature", "hot")]
    [InlineData("setDesiredTemperature", null)]
    public void SetDesiredTemperature_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setDesiredTemperature", 60)]
    [InlineData("setDesiredTemperature", 70)]
    [InlineData("setDesiredTemperature", 80)]
    public void SetDesiredTemperature_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("setColor", "red")]
    [InlineData("setColor", "#FF000")]
    [InlineData("setColor", "FF0000")]
    [InlineData("setColor", null)]
    public void SetColor_InvalidValues_Fail(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("setColor", "#FF0000")]
    [InlineData("setColor", "#aabbcc")]
    public void SetColor_ValidValues_Pass(string command, object? value)
    {
        var request = new DeviceCommandRequest(command, value);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
