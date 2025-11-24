using DynamicForm.Models;

namespace DynamicForm.Services;

public class CalculationEngineService : ICalculationEngineService
{
    public bool IsCalculationExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return false;

        // Check if expression contains mathematical operators or field references
        var operators = new[] { '+', '-', '*', '/', '(', ')', '=' };
        return operators.Any(op => expression.Contains(op)) ||
               expression.Contains("field", StringComparison.OrdinalIgnoreCase);
    }

    public object EvaluateCalculation(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        if (string.IsNullOrEmpty(expression)) return null;

        try
        {
            // Parse and evaluate the expression
            var parsedExpression = ParseExpression(expression, formValues, formDefinition);
            return EvaluateParsedExpression(parsedExpression);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating calculation: {ex.Message}");
            return null;
        }
    }

    private string ParseExpression(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        // Replace field names with their values
        var parsed = expression;

        // Find all field references (e.g., "field1", "totalAmount")
        var fieldReferences = FindFieldReferences(expression, formDefinition);

        foreach (var fieldRef in fieldReferences)
        {
            if (formValues.TryGetValue(fieldRef, out var fieldValue) && fieldValue != null)
            {
                // Convert to decimal for calculations
                if (decimal.TryParse(fieldValue.ToString(), out decimal decimalValue))
                {
                    parsed = parsed.Replace(fieldRef, decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    // If not a number, wrap in quotes for string operations
                    parsed = parsed.Replace(fieldRef, $"\"{fieldValue}\"");
                }
            }
            else
            {
                // Field not found or null value, use 0 for calculations
                parsed = parsed.Replace(fieldRef, "0");
            }
        }

        return parsed;
    }

    private List<string> FindFieldReferences(string expression, FormDefinition formDefinition)
    {
        var references = new List<string>();

        // Simple regex to find potential field names (words that aren't operators)
        var pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b";
        var matches = System.Text.RegularExpressions.Regex.Matches(expression, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var potentialField = match.Value;

            // Skip mathematical constants and operators
            if (IsMathConstant(potentialField) || IsOperator(potentialField))
                continue;

            // Check if this is actually a field in the form
            if (formDefinition.Fields.Any(f => f.Name.Equals(potentialField, StringComparison.OrdinalIgnoreCase)))
            {
                references.Add(potentialField);
            }
        }

        return references.Distinct().ToList();
    }

    private bool IsMathConstant(string value)
    {
        var constants = new[] { "pi", "e", "true", "false", "null" };
        return constants.Contains(value.ToLower());
    }

    private bool IsOperator(string value)
    {
        var operators = new[] { "and", "or", "not", "if", "else", "then" };
        return operators.Contains(value.ToLower());
    }

    private object EvaluateParsedExpression(string expression)
    {
        try
        {
            // Use DataTable.Compute for simple arithmetic
            var result = new System.Data.DataTable().Compute(expression, null);

            // Handle different result types
            return result switch
            {
                decimal decimalResult => decimalResult,
                double doubleResult => doubleResult,
                int intResult => intResult,
                bool boolResult => boolResult,
                _ => result.ToString()
            };
        }
        catch
        {
            // Fallback to custom evaluation for more complex expressions
            return EvaluateCustomExpression(expression);
        }
    }

    private object EvaluateCustomExpression(string expression)
    {
        // Simple custom evaluator for basic operations
        try
        {
            // Remove whitespace
            expression = expression.Replace(" ", "");

            // Handle basic arithmetic
            if (expression.Contains('+'))
            {
                var parts = expression.Split('+');
                return Convert.ToDecimal(parts[0]) + Convert.ToDecimal(parts[1]);
            }
            else if (expression.Contains('-'))
            {
                var parts = expression.Split('-');
                return Convert.ToDecimal(parts[0]) - Convert.ToDecimal(parts[1]);
            }
            else if (expression.Contains('*'))
            {
                var parts = expression.Split('*');
                return Convert.ToDecimal(parts[0]) * Convert.ToDecimal(parts[1]);
            }
            else if (expression.Contains('/'))
            {
                var parts = expression.Split('/');
                var divisor = Convert.ToDecimal(parts[1]);
                return divisor != 0 ? Convert.ToDecimal(parts[0]) / divisor : 0;
            }

            return expression; // Return as string if not a simple arithmetic operation
        }
        catch
        {
            return expression; // Return original expression on error
        }
    }
}
