using System.ComponentModel;
using ModelContextProtocol.Server;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for setting the simulation speed.
/// </summary>
/// <param name="simulationService">The service used to control simulation behavior.</param>
/// <param name="registry">The registry containing allowed simulation speeds.</param>
[McpServerToolType]
public sealed class SimulationSpeedTool(
    ISimulationService simulationService,
    ISimulationSpeedRegistry registry)
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    private const string ToolName = "set_simulation_speed";

    /// <summary>
    /// Sets the simulation speed multiplier.
    /// </summary>
    /// <param name="speed">Speed multiplier (1, 2, 5, or 10)</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the simulation speed operation.</returns>
    [McpServerTool(Name = ToolName)]
    [Description("Set the simulation speed multiplier. Allowed values are 1, 2, 5, or 10.")]
    public async Task<string> SetSimulationSpeedAsync(
        [Description("Speed multiplier: 1, 2, 5, or 10")]
        int speed,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(SimulationSpeed), speed))
        {
            return "Invalid simulation speed. Allowed values are 1, 2, 5, or 10.";
        }

        var simulationSpeed = (SimulationSpeed)speed;

        if (!registry.IsAllowed(simulationSpeed))
        {
            return "Invalid simulation speed. Allowed values are 1, 2, 5, or 10.";
        }

        await simulationService.SetSpeedAsync(simulationSpeed, cancellationToken);

        return $"Simulation speed set to {speed}x.";
    }
}