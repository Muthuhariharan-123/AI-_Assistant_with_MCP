using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Response from the /api/chat endpoint back to the Flutter frontend.
/// </summary>
public sealed class ChatResponse
{
    /// <summary>
    /// The AI assistant's reply text.
    /// </summary>
    [JsonPropertyName("reply")]
    public required string Reply { get; init; }

    /// <summary>
    /// Name of the MCP tool that was invoked (null if no tool was used).
    /// </summary>
    [JsonPropertyName("toolUsed")]
    public string? ToolUsed { get; init; }
}
