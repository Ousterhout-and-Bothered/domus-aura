namespace SmartHome.Api.Contracts.Chat;

/// <summary>
/// Represents a request sent to the chat endpoint containing a user message.
/// </summary>
/// <remarks>
/// This request is consumed by the chat controller and passed to the LLM service
/// to generate a response based on the provided message.
/// </remarks>
public sealed record ChatRequest
{
    /// <summary>
    /// Gets the user-provided message to be processed by the chat service.
    /// </summary>
    /// <remarks>
    /// This value is required and must not be null or empty.
    /// </remarks>
    public required string Message { get; init; }
}