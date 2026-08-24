namespace mcp_server.Models;

/// <summary>
/// Request to call a tool on the MCP server.
/// </summary>
public sealed class ToolCallRequest
{
    /// <summary>
    /// Name of the tool to invoke (e.g., "calculate").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Arguments for the tool, keyed by parameter name.
    /// </summary>
    public required Dictionary<string, string> Arguments { get; init; }
}
