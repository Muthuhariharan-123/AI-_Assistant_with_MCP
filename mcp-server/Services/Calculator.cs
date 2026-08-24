using System.Data;
using System.Text.RegularExpressions;

namespace mcp_server.Services;

/// <summary>
/// Safe math expression evaluator.
/// Uses DataTable.Compute() which supports basic arithmetic (+, -, *, /, %, parentheses)
/// but does NOT allow arbitrary code execution.
/// Input is validated against an allow-list of safe characters before evaluation.
/// </summary>
public sealed partial class Calculator
{
    // Allow-list: digits, decimal points, arithmetic operators, parentheses, spaces
    [GeneratedRegex(@"^[\d\+\-\*\/\%\.\(\)\s]+$")]
    private static partial Regex SafeExpressionPattern();

    private static readonly DataTable ComputeTable = new();

    /// <summary>
    /// Evaluates a mathematical expression safely.
    /// </summary>
    /// <param name="expression">A math expression like "12 * 7" or "(3 + 4) * 2"</param>
    /// <returns>The numeric result</returns>
    /// <exception cref="ArgumentException">If the expression contains invalid characters</exception>
    /// <exception cref="InvalidOperationException">If the expression cannot be evaluated</exception>
    public static double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression cannot be empty.");
        }

        // Enforce max length to prevent DoS via extremely long expressions
        if (expression.Length > 200)
        {
            throw new ArgumentException("Expression is too long (max 200 characters).");
        }

        // Validate against allow-list of safe characters
        if (!SafeExpressionPattern().IsMatch(expression))
        {
            throw new ArgumentException(
                "Expression contains invalid characters. Only digits, +, -, *, /, %, (, ), and . are allowed.");
        }

        try
        {
            var result = ComputeTable.Compute(expression, null);
            return Convert.ToDouble(result);
        }
        catch (Exception ex) when (ex is SyntaxErrorException or EvaluateException or DivideByZeroException or OverflowException)
        {
            throw new InvalidOperationException($"Failed to evaluate expression: {ex.Message}");
        }
    }
}
