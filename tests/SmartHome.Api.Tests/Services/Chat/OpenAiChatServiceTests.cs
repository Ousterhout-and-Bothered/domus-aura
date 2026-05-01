using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using SmartHome.Api.Services.Chat;
using SmartHome.Api.Services.Chat.Tools;

namespace SmartHome.Api.Tests.Services.Chat;

public class OpenAiChatServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IChatToolHandler> _toolHandlerMock;
    private readonly OpenAiChatService _service;

    public OpenAiChatServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();
        _toolHandlerMock = new Mock<IChatToolHandler>();

        _configurationMock.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _configurationMock.Setup(c => c["OpenAI:Model"]).Returns("gpt-4o-mini");

        _toolHandlerMock.Setup(t => t.ToolName).Returns("test_tool");
        _toolHandlerMock.Setup(t => t.ToolDefinition).Returns(new { name = "test_tool" });

        _service = new OpenAiChatService(
            _httpClient,
            _configurationMock.Object,
            new[] { _toolHandlerMock.Object });
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsContent_WhenNoToolCalls()
    {
        // Arrange
        var responseContent = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content = "Hello, how can I help you?"
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseContent));

        // Act
        var result = await _service.GetResponseAsync("Hi");

        // Assert
        Assert.Equal("Hello, how can I help you?", result);
    }

    [Fact]
    public async Task GetResponseAsync_ExecutesTool_WhenModelRequestsToolCall()
    {
        // Arrange
        var responseContent = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        tool_calls = new[]
                        {
                            new
                            {
                                function = new
                                {
                                    name = "test_tool",
                                    arguments = "{\"arg1\": \"val1\"}"
                                }
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(responseContent));
        _toolHandlerMock.Setup(t => t.HandleAsync(It.IsAny<Dictionary<string, JsonElement>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tool executed successfully.");

        // Act
        var result = await _service.GetResponseAsync("Run tool");

        // Assert
        Assert.Equal("Tool executed successfully.", result);
        _toolHandlerMock.Verify(t => t.HandleAsync(It.IsAny<Dictionary<string, JsonElement>>(), It.IsAny<CancellationToken>()), Times.Once);
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
