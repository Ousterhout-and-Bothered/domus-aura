using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp;

/// <summary>
/// Local-mode implementation of <see cref="ILlmChatService"/> that uses OpenAI's
/// function-calling API instead of the Responses + MCP API. Tools are invoked
/// in-process via reflection over types annotated with
/// <see cref="McpServerToolTypeAttribute"/>, which lets the existing MCP tool
/// classes serve double duty without modification.
/// </summary>
/// <remarks>
/// Selected via the OpenAI:Mode config setting. Local mode is the default for
/// docker compose up, because it does not require the backend to be publicly
/// reachable. Production uses MCP mode because the orchestration offload is
/// preferable when a public endpoint exists.
/// </remarks>
public sealed class OpenAiLocalChatService(
    HttpClient httpClient,
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<OpenAiLocalChatService> logger) : ILlmChatService
{
    private const int MaxToolIterations = 6;
    private const string SystemPrompt =
        "You are a smart home assistant. Use the available tools to control devices. " +
        "If the user asks for multiple actions, complete all necessary tool calls before replying. " +
        "Keep confirmations concise.";

    private static readonly Lazy<IReadOnlyList<ToolDescriptor>> Tools = new(DiscoverTools);

    public async Task<string> GetResponseAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var messages = new List<Dictionary<string, object?>>
        {
            new() { ["role"] = "system", ["content"] = SystemPrompt },
            new() { ["role"] = "user", ["content"] = message }
        };

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            var responseJson = await CallOpenAiAsync(apiKey, model, messages, cancellationToken);

            using var document = JsonDocument.Parse(responseJson);
            var choice = document.RootElement.GetProperty("choices")[0].GetProperty("message");

            // Capture the assistant message verbatim so the next iteration can reference its tool_calls.
            var assistantMessage = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                choice.GetRawText())!;
            messages.Add(assistantMessage);

            if (!choice.TryGetProperty("tool_calls", out var toolCalls)
                || toolCalls.ValueKind != JsonValueKind.Array
                || toolCalls.GetArrayLength() == 0)
            {
                return choice.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
                    ? content.GetString() ?? string.Empty
                    : string.Empty;
            }

            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                var callId = toolCall.GetProperty("id").GetString()!;
                var function = toolCall.GetProperty("function");
                var name = function.GetProperty("name").GetString()!;
                var argumentsJson = function.GetProperty("arguments").GetString() ?? "{}";

                var resultText = await InvokeToolAsync(name, argumentsJson, cancellationToken);

                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = callId,
                    ["content"] = resultText
                });
            }
        }

        logger.LogWarning(
            "Hit MaxToolIterations ({Limit}) without a final assistant response.",
            MaxToolIterations);

        return "I had trouble completing that request. Please try again.";
    }

    private async Task<string> CallOpenAiAsync(
        string apiKey,
        string model,
        IReadOnlyList<Dictionary<string, object?>> messages,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            messages,
            tools = Tools.Value.Select(t => t.ToOpenAiSchema()).ToArray(),
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

        return json;
    }

    private async Task<string> InvokeToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var descriptor = Tools.Value.FirstOrDefault(t => t.Name == toolName);

        if (descriptor is null)
        {
            return $"Tool {toolName} is not available.";
        }

        try
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, descriptor.DeclaringType);
            var arguments = ParseArguments(descriptor, argumentsJson, cancellationToken);
            var result = descriptor.Method.Invoke(instance, arguments);

            if (result is Task<string> stringTask)
            {
                return await stringTask;
            }

            if (result is Task task)
            {
                await task;
                return "Done.";
            }

            return result?.ToString() ?? string.Empty;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            logger.LogError(ex.InnerException, "Tool {Tool} threw an exception.", toolName);
            return $"Tool {toolName} failed: {ex.InnerException.Message}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool {Tool} could not be invoked.", toolName);
            return $"Tool {toolName} failed.";
        }
    }

    private static object?[] ParseArguments(
        ToolDescriptor descriptor,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var root = document.RootElement;

        var values = new object?[descriptor.Parameters.Count];

        for (var i = 0; i < descriptor.Parameters.Count; i++)
        {
            var parameter = descriptor.Parameters[i];

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                values[i] = cancellationToken;
                continue;
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(parameter.Name!, out var value))
            {
                values[i] = JsonSerializer.Deserialize(value.GetRawText(), parameter.ParameterType);
                continue;
            }

            values[i] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
        }

        return values;
    }

    private static IReadOnlyList<ToolDescriptor> DiscoverTools()
    {
        var assembly = typeof(OpenAiLocalChatService).Assembly;
        var descriptors = new List<ToolDescriptor>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is null)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (toolAttr is null) continue;

                descriptors.Add(new ToolDescriptor(
                    Name: toolAttr.Name ?? method.Name,
                    Description: method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    DeclaringType: type,
                    Method: method,
                    Parameters: method.GetParameters()));
            }
        }

        return descriptors;
    }

    private sealed record ToolDescriptor(
        string Name,
        string Description,
        Type DeclaringType,
        MethodInfo Method,
        IReadOnlyList<ParameterInfo> Parameters)
    {
        public object ToOpenAiSchema()
        {
            var properties = new Dictionary<string, object>();
            var required = new List<string>();

            foreach (var p in Parameters)
            {
                if (p.ParameterType == typeof(CancellationToken)) continue;

                properties[p.Name!] = new
                {
                    type = JsonSchemaTypeFor(p.ParameterType),
                    description = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty
                };

                if (!p.HasDefaultValue)
                {
                    required.Add(p.Name!);
                }
            }

            return new
            {
                type = "function",
                function = new
                {
                    name = Name,
                    description = Description,
                    parameters = new
                    {
                        type = "object",
                        properties,
                        required
                    }
                }
            };
        }

        private static string JsonSchemaTypeFor(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "integer";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "number";
            if (t == typeof(bool)) return "boolean";
            return "string";
        }
    }
}