using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Incoming chat request from the Flutter frontend.
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// The user's message (max 2000 characters).
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
