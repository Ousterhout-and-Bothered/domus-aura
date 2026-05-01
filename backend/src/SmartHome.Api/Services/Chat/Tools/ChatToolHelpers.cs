using System.Text.Json;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Provides shared helper methods for parsing chat tool arguments,
/// formatting responses, and executing commands against powered devices.
/// </summary>
internal static class ChatToolHelpers
{
    /// <summary>
    /// Determines whether the supplied value targets every matching device.
    /// </summary>
    /// <param name="value">The value to compare against all.</param>
    /// <returns><see langword="true"/> when the value is all; otherwise, <see langword="false"/>.</returns>
    public static bool IsAll(string? value) =>
        string.Equals(value, "all", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a requested location into a repository filter value.
    /// </summary>
    /// <param name="location">The requested location, or all.</param>
    /// <returns><see langword="null"/> when all locations should be included; otherwise, the requested location.</returns>
    public static string? ToLocationFilter(string? location) =>
        IsAll(location) ? null : location;

    /// <summary>
    /// Attempts to read a non-empty string argument from a tool call.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="name">The argument name to read.</param>
    /// <param name="value">The parsed string value when the argument is present and non-empty.</param>
    /// <returns><see langword="true"/> when a non-empty string value is found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetString(
        Dictionary<string, JsonElement> arguments,
        string name,
        out string? value)
    {
        value = null;

        if (!arguments.TryGetValue(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            value = element.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    /// <summary>
    /// Attempts to read an integer argument from a tool call.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="name">The argument name to read.</param>
    /// <param name="value">The parsed integer value when the argument is present and valid.</param>
    /// <returns><see langword="true"/> when a valid integer value is found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetInt(
        Dictionary<string, JsonElement> arguments,
        string name,
        out int value)
    {
        value = 0;

        if (!arguments.TryGetValue(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        if (element.ValueKind == JsonValueKind.String)
            return int.TryParse(element.GetString(), out value);

        return false;
    }

    /// <summary>
    /// Formats a count and noun using a simple singular or plural form.
    /// </summary>
    /// <param name="count">The number of items.</param>
    /// <param name="noun">The singular noun to format.</param>
    /// <returns>A formatted count phrase.</returns>
    public static string Pluralize(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>
    /// Formats a count and noun as the subject of a sentence with the correct verb form.
    /// </summary>
    /// <param name="count">The number of items.</param>
    /// <param name="noun">The singular noun to format.</param>
    /// <returns>A formatted sentence subject using 'was' or 'were'.</returns>
    public static string SentenceCount(int count, string noun) =>
        count == 1 ? $"1 {noun} was" : $"{count} {noun}s were";

    /// <summary>
    /// Represents the result of applying a powered-device tool command to one or more devices.
    /// </summary>
    internal sealed class PoweredToolExecutionResult
    {
        /// <summary>
        /// Gets the number of devices changed by the command.
        /// </summary>
        public int Changed { get; init; }

        /// <summary>
        /// Gets the number of devices that were already in the requested state.
        /// </summary>
        public int Unchanged { get; init; }

        /// <summary>
        /// Gets the number of devices skipped because they were powered off.
        /// </summary>
        public int PoweredOff { get; init; }
    }

    /// <summary>
    /// Executes a command against powered devices that are on and not already in the requested state.
    /// </summary>
    /// <param name="devices">The devices to evaluate.</param>
    /// <param name="isAlreadyCorrect">A predicate that determines whether a device is already in the requested state.</param>
    /// <param name="executeCommand">The command execution delegate to run for each eligible device.</param>
    /// <returns>A summary of changed, unchanged, and powered-off devices.</returns>
    public static async Task<PoweredToolExecutionResult> ExecuteOnPoweredDevicesAsync(
        IEnumerable<Device> devices,
        Func<Device, bool> isAlreadyCorrect,
        Func<Device, Task> executeCommand)
    {
        var changed = 0;
        var unchanged = 0;
        var poweredOff = 0;

        foreach (var device in devices)
        {
            if (!device.IsOn())
            {
                poweredOff++;
                continue;
            }

            if (isAlreadyCorrect(device))
            {
                unchanged++;
                continue;
            }

            await executeCommand(device);
            changed++;
        }

        return new PoweredToolExecutionResult
        {
            Changed = changed,
            Unchanged = unchanged,
            PoweredOff = poweredOff
        };
    }
}