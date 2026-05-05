using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Api.Contracts.Chat;
using SmartHome.Api.Controller;
using SmartHome.Api.Services.Chat;
using Xunit;

namespace SmartHome.Api.Tests.Controller;

public class ChatControllerTests
{
    private readonly Mock<ILlmChatService> _llmChatServiceMock;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _llmChatServiceMock = new Mock<ILlmChatService>();
        _controller = new ChatController(_llmChatServiceMock.Object);
    }

    [Fact]
    public async Task SendMessage_ReturnsOk_WithResponse()
    {
        // Arrange
        var request = new ChatRequest { Message = "Hello" };
        _llmChatServiceMock.Setup(s => s.GetResponseAsync("Hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hi there!");

        // Act
        var result = await _controller.SendMessage(request, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatResponse>(okResult.Value);
        Assert.Equal("Hi there!", response.Response);
    }

    [Fact]
    public async Task SendMessage_ReturnsBadRequest_WhenMessageEmpty()
    {
        // Arrange
        var request = new ChatRequest { Message = "" };

        // Act
        var result = await _controller.SendMessage(request, default);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
    }
}
