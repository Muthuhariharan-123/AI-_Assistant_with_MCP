using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services;

/// <summary>
/// Client for the Google Gemini API with function-calling support.
/// Uses the generateContent endpoint with tool definitions.
/// </summary>
public sealed class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _apiKey;
    private const string ModelId = "gemini-3.6-flash";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";


    public GeminiClient(HttpClient httpClient, ILogger<GeminiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException(
                "GEMINI_API_KEY is not configured. Set it as an environment variable or in .env file.");
    }

    /// <summary>
    /// Sends a message to Gemini and returns its response.
    /// If Gemini wants to call a tool, this returns a GeminiResult with the function call info.
    /// If Gemini returns a text response, this returns a GeminiResult with the text.
    /// </summary>
    public async Task<GeminiResult> SendMessageAsync(string userMessage)
    {
        var requestBody = BuildRequest(userMessage);
        return await CallGeminiAsync(requestBody);
    }

    /// <summary>
    /// Sends the tool result back to Gemini to get the final natural-language response.
    /// </summary>
    public async Task<GeminiResult> SendToolResultAsync(
        string userMessage,
        GeminiResult geminiResult,
        string toolResult)
    {
        var requestBody = BuildRequestWithToolResult(userMessage, geminiResult, toolResult);
        return await CallGeminiAsync(requestBody);
    }

    private async Task<GeminiResult> CallGeminiAsync(object requestBody)
    {
        var url = $"{BaseUrl}/models/{ModelId}:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Response}",
                response.StatusCode, responseContent);
            throw new InvalidOperationException(
                $"Gemini API returned {response.StatusCode}. Check your API key and quota.");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (geminiResponse?.Candidates is null || geminiResponse.Candidates.Count == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates.");
        }

        var candidate = geminiResponse.Candidates[0];
        var parts = candidate.Content?.Parts;

        if (parts is null || parts.Count == 0)
        {
            throw new InvalidOperationException("Gemini returned empty content.");
        }

        // Check if the response contains a function call
        var functionCallPart = parts.FirstOrDefault(p => p.TryGetProperty("functionCall", out _));
        if (functionCallPart.ValueKind != JsonValueKind.Undefined)
        {
            var fc = functionCallPart.GetProperty("functionCall");
            var name = fc.GetProperty("name").GetString() ?? "";
            var args = fc.TryGetProperty("args", out var argsElement) ? argsElement.GetRawText() : "{}";
            _logger.LogInformation("Gemini requested tool call: {FunctionName}", name);
            return GeminiResult.FunctionCallResult(name, args, functionCallPart);
        }

        // Otherwise it's a text response
        var textPart = parts.FirstOrDefault(p => p.TryGetProperty("text", out _));
        var text = textPart.ValueKind != JsonValueKind.Undefined ? textPart.GetProperty("text").GetString() : null;
        return GeminiResult.TextResult(text ?? "I couldn't generate a response.");
    }

    private static object BuildRequest(string userMessage)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                }
            },
            tools = new[] { GetToolDefinition() },
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = "You are a helpful AI assistant. When the user asks a math question, " +
                               "use the 'calculate' function to compute the answer. " +
                               "Always use the calculator for any arithmetic — do not try to calculate mentally."
                    }
                }
            }
        };
    }

    private static object BuildRequestWithToolResult(
        string userMessage,
        GeminiResult geminiResult,
        string toolResult)
    {
        return new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                },
                new
                {
                    role = "model",
                    parts = new object[]
                    {
                        geminiResult.OriginalPart
                    }
                },
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            functionResponse = new
                            {
                                name = geminiResult.FunctionName,
                                response = new
                                {
                                    result = toolResult
                                }
                            }
                        }
                    }
                }
            },
            tools = new[] { GetToolDefinition() },
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = "You are a helpful AI assistant. When the user asks a math question, " +
                               "use the 'calculate' function to compute the answer. " +
                               "Always use the calculator for any arithmetic — do not try to calculate mentally."
                    }
                }
            }
        };
    }

    private static object GetToolDefinition()
    {
        return new
        {
            functionDeclarations = new[]
            {
                new
                {
                    name = "calculate",
                    description = "Evaluates a mathematical expression and returns the numeric result. " +
                                  "Supports +, -, *, /, %, parentheses, and decimal numbers.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            expression = new
                            {
                                type = "string",
                                description = "The math expression to evaluate, e.g. '12 * 7' or '(3 + 4) * 2'"
                            }
                        },
                        required = new[] { "expression" }
                    }
                }
            }
        };
    }
}

/// <summary>
/// Represents the result of a Gemini API call — either text or a function call request.
/// </summary>
public sealed class GeminiResult
{
    public string? Text { get; private init; }
    public string? FunctionName { get; private init; }
    public string? FunctionArgs { get; private init; }
    public JsonElement? OriginalPart { get; private init; }
    public bool IsFunctionCall => FunctionName is not null;

    public static GeminiResult TextResult(string text) => new() { Text = text };
    public static GeminiResult FunctionCallResult(string name, string args, JsonElement original) =>
        new() { FunctionName = name, FunctionArgs = args, OriginalPart = original };
}

// Gemini API response DTOs
internal sealed class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; init; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; init; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<JsonElement>? Parts { get; init; }
}
