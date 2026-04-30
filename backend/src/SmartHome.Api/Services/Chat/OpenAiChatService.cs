using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartHome.Api.Services.Chat.Tools;

namespace SmartHome.Api.Services.Chat;

/// <summary>
/// Provides an OpenAI-backed implementation of <see cref="ILlmChatService"/>
/// for interpreting smart home chat requests and invoking registered chat tools.
/// </summary>
/// <param name="httpClient">The HTTP client used to send requests to the OpenAI API.</param>
/// <param name="configuration">The application configuration containing OpenAI settings.</param>
/// <param name="toolHandlers">The registered chat tool handlers available to the language model.</param>
public sealed class OpenAiChatService(
    HttpClient httpClient,
    IConfiguration configuration,
    IEnumerable<IChatToolHandler> toolHandlers) : ILlmChatService
{
    private readonly IReadOnlyDictionary<string, IChatToolHandler> _toolHandlers =
        toolHandlers.ToDictionary(
            handler => handler.ToolName,
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sends a user message to OpenAI and returns either the model response
    /// or the combined results of any requested tool calls.
    /// </summary>
    /// <param name="message">The user message to process.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The model response or tool execution result summary.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the OpenAI API key is missing or the OpenAI request fails.
    /// </exception>
    public async Task<string> GetResponseAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a smart home assistant. Use tools to control devices. " +
                              "If the user asks for multiple actions, call all necessary tools " +
                              "in the same response."
                },
                new
                {
                    role = "user",
                    content = message
                }
            },
            tools = _toolHandlers.Values
                .Select(handler => handler.ToolDefinition)
                .ToArray(),
            tool_choice = "auto"
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI request failed: {response.StatusCode}. Body: {json}");
        }

        using var document = JsonDocument.Parse(json);

        var messageElement = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        if (messageElement.TryGetProperty("tool_calls", out var toolCalls))
        {
            var results = new List<string>();

            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                var functionName = toolCall
                    .GetProperty("function")
                    .GetProperty("name")
                    .GetString();

                var argumentsJson = toolCall
                    .GetProperty("function")
                    .GetProperty("arguments")
                    .GetString();

                var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    argumentsJson ?? "{}");

                if (functionName is not null &&
                    args is not null &&
                    _toolHandlers.TryGetValue(functionName, out var handler))
                {
                    var result = await handler.HandleAsync(args, cancellationToken);
                    results.Add(result);
                    continue;
                }

                results.Add($"I received an unsupported tool request: {functionName}.");
            }

            return string.Join(" ", results);
        }

        return messageElement.GetProperty("content").GetString() ?? "No response";
    }
}