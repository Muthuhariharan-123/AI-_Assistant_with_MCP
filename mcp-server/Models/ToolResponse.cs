namespace mcp_server.Models;

/// <summary>
/// Response from a tool call on the MCP server.
/// </summary>
public sealed class ToolCallResponse
{
    /// <summary>
    /// The computed result (null if an error occurred).
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Error message if the tool call failed (null on success).
    /// </summary>
    public string? Error { get; init; }

    public static ToolCallResponse Success(object result) => new() { Result = result };
    public static ToolCallResponse Failure(string error) => new() { Error = error };
}

/// <summary>
/// Describes a tool available on this MCP server.
/// </summary>
public sealed class ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Dictionary<string, ParameterDefinition> Parameters { get; init; }
}

/// <summary>
/// Describes a single parameter of a tool.
/// </summary>
public sealed class ParameterDefinition
{
    public required string Type { get; init; }
    public required string Description { get; init; }
}
