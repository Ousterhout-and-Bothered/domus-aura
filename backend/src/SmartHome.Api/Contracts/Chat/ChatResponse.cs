namespace SmartHome.Api.Contracts.Chat;

public sealed record ChatResponse
{
    public required string Response { get; init; }
}