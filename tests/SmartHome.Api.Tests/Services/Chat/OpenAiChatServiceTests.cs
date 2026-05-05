using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using SmartHome.Api.Services.Chat.Mcp;

namespace SmartHome.Api.Tests.Services.Chat;

public class OpenAiChatServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly OpenAiChatService _service;

    public OpenAiChatServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _configurationMock.Setup(c => c["OpenAI:Model"]).Returns("gpt-4o-mini");
        _configurationMock.Setup(c => c["OpenAI:McpServerUrl"]).Returns("http://test-mcp-server");

        _service = new OpenAiChatService(
            _httpClient,
            _configurationMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsOutputText_WhenPresent()
    {
        // Arrange
        var responseContent = new
        {
            output_text = "Hello, how can I help you?"
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseContent));

        // Act
        var result = await _service.GetResponseAsync("Hi");

        // Assert
        Assert.Equal("Hello, how can I help you?", result);
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsContentText_WhenOutputTextMissing()
    {
        // Arrange
        var responseContent = new
        {
            output = new[]
            {
                new
                {
                    content = new[]
                    {
                        new { text = "Nested response text" }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseContent));

        // Act
        var result = await _service.GetResponseAsync("Hi");

        // Assert
        Assert.Equal("Nested response text", result);
    }

    [Fact]
    public async Task GetResponseAsync_ThrowsException_WhenApiKeyMissing()
    {
        // Arrange
        _configurationMock.Setup(c => c["OpenAI:ApiKey"]).Returns(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetResponseAsync("Hi"));
    }

    [Fact]
    public async Task GetResponseAsync_ThrowsException_WhenMcpServerUrlMissing()
    {
        // Arrange
        _configurationMock.Setup(c => c["OpenAI:McpServerUrl"]).Returns(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetResponseAsync("Hi"));
    }

    [Fact]
    public async Task GetResponseAsync_ThrowsException_WhenApiFails()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetResponseAsync("Hi"));
        Assert.Contains("OpenAI request failed", exception.Message);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}
