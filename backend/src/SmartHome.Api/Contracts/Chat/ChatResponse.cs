namespace SmartHome.Api.Contracts.Chat;

/// <summary>
/// Represents the response returned from the chat endpoint.
/// </summary>
/// <remarks>
/// This response contains the generated reply from the language model
/// after processing the user's input message.
/// </remarks>
public sealed record ChatResponse
{
    /// <summary>
    /// Gets the generated response message from the chat service.
    /// </summary>
    /// <remarks>
    /// This value is required and contains the final text returned to the client.
    /// </remarks>
    public required string Response { get; init; }
}