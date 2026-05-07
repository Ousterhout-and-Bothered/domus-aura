using System.ComponentModel;
using ModelContextProtocol.Server;
using SmartHome.Domain.Scene;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for executing a scene.
/// </summary>
/// <param name="sceneService">The service used to retrieve and execute scenes.</param>
[McpServerToolType]
public sealed class SceneTool(
    ISceneService sceneService)
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    private const string ToolName = "set_scene";

    /// <summary>
    /// Executes a scene by name.
    /// </summary>
    /// <param name="name">Scene name like "Movie Night" or "Good Night"</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the scene execution.</returns>
    [McpServerTool(Name = ToolName)]
    [Description("Execute a scene by name, such as 'Movie Night' or 'Good Night'.")]
    public async Task<string> ExecuteSceneAsync(
        [Description("Scene name like 'Movie Night' or 'Good Night'")]
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "I need a scene name to execute.";
        }

        var scenes = await sceneService.GetAllScenesAsync(cancellationToken);

        var scene = scenes.FirstOrDefault(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

        if (scene is null)
        {
            return $"I could not find a scene named {name}.";
        }

        var result = await sceneService.ExecuteSceneAsync(scene.Id, cancellationToken);

        var successCount = result.Entries.Count(e => e.Result.Success);
        var failedCount = result.Entries.Count(e => !e.Result.Success);

        if (failedCount == 0)
        {
            return $"Executed scene '{scene.Name}'. {successCount} actions completed.";
        }

        return $"Executed scene '{scene.Name}'. {successCount} succeeded, {failedCount} failed.";
    }
}