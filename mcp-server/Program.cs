using mcp_server.Models;
using mcp_server.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 3001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3001);
});

var app = builder.Build();

// Tool definitions — the only tools this MCP server exposes
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
    },
    new()
    {
        Name = "get_weather",
        Description = "Gets the current weather for a specific location.",
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["location"] = new()
            {
                Type = "string",
                Description = "The city and country to get the weather for, e.g. 'London, UK' or 'New York'"
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
app.MapPost("/tools/call", async (ToolCallRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(ToolCallResponse.Failure("Tool name is required."));
    }

    try
    {
        if (string.Equals(request.Name, "calculate", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.Arguments.TryGetValue("expression", out var expression) || string.IsNullOrWhiteSpace(expression))
            {
                return Results.BadRequest(ToolCallResponse.Failure("Missing required argument: 'expression'."));
            }
            var result = Calculator.Evaluate(expression);
            return Results.Ok(ToolCallResponse.Success(result));
        }
        else if (string.Equals(request.Name, "get_weather", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.Arguments.TryGetValue("location", out var location) || string.IsNullOrWhiteSpace(location))
            {
                return Results.BadRequest(ToolCallResponse.Failure("Missing required argument: 'location'."));
            }
            var result = await WeatherService.GetWeatherAsync(location);
            return Results.Ok(ToolCallResponse.Success(result));
        }
        else
        {
            return Results.NotFound(ToolCallResponse.Failure($"Unknown tool: {request.Name}"));
        }
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
