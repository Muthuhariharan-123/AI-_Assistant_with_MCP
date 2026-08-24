using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services;

/// <summary>
/// HTTP client for the MCP calculator server.
/// Calls the mcp-server container's /tools/call endpoint.
/// </summary>
public sealed class McpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClient> _logger;

    public McpClient(HttpClient httpClient, ILogger<McpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Calls the "calculate" tool on the MCP server.
    /// </summary>
    /// <param name="expression">Math expression to evaluate</param>
    /// <returns>The string representation of the result, or an error message</returns>
    public async Task<string> CalculateAsync(string expression)
    {
        var request = new McpToolCallRequest
        {
            Name = "calculate",
            Arguments = new Dictionary<string, string> { ["expression"] = expression }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/tools/call", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<McpToolCallResponse>();

            if (result?.Error is not null)
            {
                _logger.LogWarning("MCP tool returned error: {Error}", result.Error);
                return $"Error: {result.Error}";
            }

            return result?.Result?.ToString() ?? "No result";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call MCP server");
            return "Error: Calculator service is unavailable.";
        }
    }

    // Internal DTOs for MCP server communication
    private sealed class McpToolCallRequest
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("arguments")]
        public required Dictionary<string, string> Arguments { get; init; }
    }

    private sealed class McpToolCallResponse
    {
        [JsonPropertyName("result")]
        public double? Result { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
