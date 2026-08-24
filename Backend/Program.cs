using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Backend.Models;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file if it exists
// (for local development — in Docker, env vars are injected by docker-compose)
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFilePath))
{
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            continue;

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
            continue;

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        Environment.SetEnvironmentVariable(key, value);
    }
}

// Register services
builder.Services.AddSingleton<GeminiClient>();
builder.Services.AddHttpClient<McpClient>(client =>
{
    var mcpUrl = builder.Configuration["MCP_SERVER_URL"] ?? "http://localhost:3001";
    client.BaseAddress = new Uri(mcpUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<ChatService>();

// CORS — allow frontend origin only
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // In Docker: frontend is served at localhost:8080
        // For local dev: Flutter web typically runs at localhost:8081 or similar
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:8081",
                "http://127.0.0.1:8080",
                "http://127.0.0.1:8081")
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "OPTIONS");
    });
});

// Rate limiting — protect the /api/chat endpoint
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("chat", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "0"); // Modern browsers: disable legacy XSS filter
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");
    // TODO(security): Add Content-Security-Policy header for production deployment
    // TODO(security): Enable HTTPS/TLS termination via reverse proxy for production
    await next();
});

app.UseCors();
app.UseRateLimiter();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Main chat endpoint
app.MapPost("/api/chat", async (ChatRequest request, ChatService chatService) =>
{
    // Input validation
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message cannot be empty." });
    }

    if (request.Message.Length > 2000)
    {
        return Results.BadRequest(new { error = "Message too long (max 2000 characters)." });
    }

    var response = await chatService.ProcessMessageAsync(request.Message);
    return Results.Ok(response);
})
.RequireRateLimiting("chat");

app.Run();
