using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Contracts.Chat;
using SmartHome.Api.Services.Chat.Mcp;

namespace SmartHome.Api.Controller;

/// <summary>
/// Provides API endpoints for interacting with the smart home chat assistant.
/// </summary>
/// <param name="llmChatService">The service used to process chat messages and generate responses.</param>
[ApiController]
[Route("api/chat")]
[Authorize]
[Produces("application/json")]
public sealed class ChatController(
    ILlmChatService llmChatService) : ControllerBase
{
    /// <summary>
    /// Sends a user message to the chat assistant and returns the generated response.
    /// </summary>
    /// <param name="request">The chat request containing the user message.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The generated chat response.</returns>
    /// <response code="200">The chat response was successfully generated.</response>
    /// <response code="400">The request message was missing or invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        var response = await llmChatService.GetResponseAsync(
            request.Message,
            cancellationToken);

        return Ok(new ChatResponse
        {
            Response = response
        });
    }
}