using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Contracts.Chat;
using SmartHome.Api.Services.Chat;

namespace SmartHome.Api.Controller;

[ApiController]
[Route("api/chat")]
// [Authorize]
[Produces("application/json")]
public sealed class ChatController(
    ILlmChatService llmChatService) : ControllerBase
{
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