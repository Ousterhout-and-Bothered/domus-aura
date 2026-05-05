using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartHome.Api.Services.Chat.Mcp;

/// <summary>
/// Provides an OpenAI-backed implementation of <see cref="ILlmChatService"/>
/// that connects the model to the Smart Home MCP server.
/// </summary>
/// <param name="httpClient">The HTTP client used to send requests to the OpenAI API.</param>
/// <param name="configuration">The application configuration containing OpenAI settings.</param>
public sealed class OpenAiChatService(
    HttpClient httpClient,
    IConfiguration configuration) : ILlmChatService
{
    /// <summary>
    /// Sends a user message to OpenAI and allows the model to invoke Smart Home MCP tools.
    /// </summary>
    /// <param name="message">The user message to process.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The model's final response after any MCP tool calls are completed.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when OpenAI configuration is missing or the OpenAI request fails.
    /// </exception>
    public async Task<string> GetResponseAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini";
        var mcpServerUrl = configuration["OpenAI:McpServerUrl"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(mcpServerUrl))
        {
            throw new InvalidOperationException("OpenAI MCP server URL is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/responses");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            instructions = "You are a smart home assistant. Use the available MCP tools to control devices. " +
                           "If the user asks for multiple actions, complete all necessary actions before replying. " +
                           "Keep confirmations concise.",
            input = message,
            tools = new object[]
            {
                new
                {
                    type = "mcp",
                    server_label = "smart_home",
                    server_description = "Smart home device control tools for lights, fans, thermostats, door locks, and scenes.",
                    server_url = mcpServerUrl,
                    require_approval = "never"
                }
            }
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

        return ExtractOutputText(json);
    }

    private static string ExtractOutputText(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("output_text", out var outputText))
        {
            var text = outputText.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        if (!document.RootElement.TryGetProperty("output", out var output))
        {
            return "No response.";
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
        }

        return "No response.";
    }
}