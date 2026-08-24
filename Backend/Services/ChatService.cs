using System.Text.Json;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Orchestrates the chat flow: receives user message → calls Gemini LLM →
/// if tool call requested → calls MCP server → sends result back to Gemini →
/// returns final response.
/// </summary>
public sealed class ChatService
{
    private readonly GeminiClient _geminiClient;
    private readonly McpClient _mcpClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(GeminiClient geminiClient, McpClient mcpClient, ILogger<ChatService> logger)
    {
        _geminiClient = geminiClient;
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Processes a chat message through the LLM + MCP pipeline.
    /// </summary>
    public async Task<ChatResponse> ProcessMessageAsync(string userMessage)
    {
        _logger.LogInformation("Processing chat message (length: {Length})", userMessage.Length);

        try
        {
            // Step 1: Send the user message to Gemini
            var geminiResult = await _geminiClient.SendMessageAsync(userMessage);

            // Step 2: If Gemini wants to call a tool, execute it
            if (geminiResult.IsFunctionCall)
            {
                return await HandleToolCallAsync(userMessage, geminiResult);
            }

            // Step 3: No tool call — return the text response directly
            return new ChatResponse
            {
                Reply = geminiResult.Text ?? "I couldn't generate a response.",
                ToolUsed = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            // Return a generic error message — don't expose internal details to client
            return new ChatResponse
            {
                Reply = "Sorry, I encountered an error processing your request. Please try again.",
                ToolUsed = null
            };
        }
    }

    private async Task<ChatResponse> HandleToolCallAsync(string userMessage, GeminiResult geminiResult)
    {
        var functionName = geminiResult.FunctionName!;
        var functionArgs = geminiResult.FunctionArgs!;

        _logger.LogInformation("LLM requested tool call: {ToolName}", functionName);

        // Extract the expression from function arguments
        string toolResult;
        if (string.Equals(functionName, "calculate", StringComparison.OrdinalIgnoreCase))
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, string>>(functionArgs);
            var expression = args?.GetValueOrDefault("expression") ?? "";
            toolResult = await _mcpClient.CalculateAsync(expression);
        }
        else
        {
            _logger.LogWarning("Unknown tool requested: {ToolName}", functionName);
            toolResult = $"Error: Unknown tool '{functionName}'.";
        }

        _logger.LogInformation("Tool result: {Result}", toolResult);

        // Step 3: Send the tool result back to Gemini for the final response
        var finalResult = await _geminiClient.SendToolResultAsync(
            userMessage, geminiResult, toolResult);

        return new ChatResponse
        {
            Reply = finalResult.Text ?? "I couldn't generate a response.",
            ToolUsed = functionName
        };
    }
}
