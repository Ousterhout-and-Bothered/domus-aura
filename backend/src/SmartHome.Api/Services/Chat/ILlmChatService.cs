namespace SmartHome.Api.Services.Chat;

public interface ILlmChatService
{
    Task<string> GetResponseAsync(string message, CancellationToken cancellationToken = default);
}