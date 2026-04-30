namespace SmartHome.Api.Contracts.Chat;

public sealed record ChatRequest
{
    public required string Message { get; init; }
}