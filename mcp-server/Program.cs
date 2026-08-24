using mcp_server.Models;
using mcp_server.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 3001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3001);
});

var app = builder.Build();

// Tool definitions — the only tool this MCP server exposes
var tools = new List<ToolDefinition>
{
    new()
    {
        Name = "calculate",
        Description = "Evaluates a mathematical expression and returns the numeric result. " +
                      "Supports +, -, *, /, %, parentheses, and decimal numbers.",
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["expression"] = new()
            {
                Type = "string",
                Description = "The math expression to evaluate, e.g. '12 * 7' or '(3 + 4) * 2'"
            }
        }
    }
};

/// <summary>
/// POST /tools/list — returns the list of tools available on this MCP server.
/// </summary>
app.MapPost("/tools/list", () => Results.Ok(new { tools }));

/// <summary>
/// POST /tools/call — executes a tool by name with the given arguments.
/// </summary>
app.MapPost("/tools/call", (ToolCallRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(ToolCallResponse.Failure("Tool name is required."));
    }

    if (!string.Equals(request.Name, "calculate", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound(ToolCallResponse.Failure($"Unknown tool: {request.Name}"));
    }

    if (!request.Arguments.TryGetValue("expression", out var expression) ||
        string.IsNullOrWhiteSpace(expression))
    {
        return Results.BadRequest(ToolCallResponse.Failure("Missing required argument: 'expression'."));
    }

    try
    {
        var result = Calculator.Evaluate(expression);
        return Results.Ok(ToolCallResponse.Success(result));
    }
    catch (ArgumentException ex)
    {
        // Input validation failures — return 400
        return Results.BadRequest(ToolCallResponse.Failure(ex.Message));
    }
    catch (InvalidOperationException ex)
    {
        // Evaluation failures — return 422
        return Results.UnprocessableEntity(ToolCallResponse.Failure(ex.Message));
    }
});

/// <summary>
/// Health check endpoint.
/// </summary>
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
