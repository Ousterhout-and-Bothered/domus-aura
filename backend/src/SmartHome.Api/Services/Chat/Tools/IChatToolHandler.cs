using System.Text.Json;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Defines a handler for a chat tool that can be exposed to the language model
/// and executed by the API when the model requests a tool call.
/// </summary>
public interface IChatToolHandler
{
    /// <summary>
    /// Gets the unique tool name used by the language model when invoking this handler.
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// Gets the tool definition sent to the language model, including the tool name,
    /// description, parameters, and required arguments.
    /// </summary>
    object ToolDefinition { get; }

    /// <summary>
    /// Executes the tool using the arguments supplied by the language model.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the tool execution.</returns>
    Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default);
}