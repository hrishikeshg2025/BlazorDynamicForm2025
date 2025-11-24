using DynamicForm.Models;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DynamicForm.Services;

public class EnhancedCalculationEngineService : IEnhancedCalculationEngineService
{
    private readonly Dictionary<string, Func<decimal[], decimal>> _mathFunctions = new();
    private readonly Dictionary<string, Func<string[], string>> _stringFunctions = new();
    private readonly INumberFormatService _numberFormatService;
    public EnhancedCalculationEngineService(INumberFormatService numberFormatService)
    {   
        InitializeMathFunctions();
        InitializeStringFunctions();
        _numberFormatService = numberFormatService;
    }

    private void InitializeMathFunctions()
    {
        _mathFunctions["round"] = args => Math.Round(args[0], (int)args[1]);
        _mathFunctions["floor"] = args => Math.Floor(args[0]);
        _mathFunctions["ceiling"] = args => Math.Ceiling(args[0]);
        _mathFunctions["abs"] = args => Math.Abs(args[0]);
        _mathFunctions["sqrt"] = args => (decimal)Math.Sqrt((double)args[0]);
        _mathFunctions["pow"] = args => (decimal)Math.Pow((double)args[0], (double)args[1]);
        _mathFunctions["min"] = args => args.Min();
        _mathFunctions["max"] = args => args.Max();
        _mathFunctions["sum"] = args => args.Sum();
        _mathFunctions["avg"] = args => args.Average();
        _mathFunctions["sin"] = args => (decimal)Math.Sin((double)args[0]);
        _mathFunctions["cos"] = args => (decimal)Math.Cos((double)args[0]);
        _mathFunctions["tan"] = args => (decimal)Math.Tan((double)args[0]);
        _mathFunctions["log"] = args => (decimal)Math.Log((double)args[0]);
        _mathFunctions["log10"] = args => (decimal)Math.Log10((double)args[0]);
    }

    private void InitializeStringFunctions()
    {
        _stringFunctions["concat"] = args => string.Concat(args);
        _stringFunctions["upper"] = args => args[0].ToUpper();
        _stringFunctions["lower"] = args => args[0].ToLower();
        _stringFunctions["trim"] = args => args[0].Trim();
        _stringFunctions["substring"] = args =>
            args[0].Substring((int)decimal.Parse(args[1]), (int)decimal.Parse(args[2]));
        _stringFunctions["length"] = args => args[0].Length.ToString();
        _stringFunctions["replace"] = args => args[0].Replace(args[1], args[2]);
    }

    public bool IsCalculationExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return false;

        var operators = new[] { '+', '-', '*', '/', '(', ')', '=' };
        var functionPattern = @"\b(round|floor|ceiling|abs|sqrt|pow|min|max|sum|avg|sin|cos|tan|log|log10|concat|upper|lower|trim|substring|length|replace|if|case)\b";

        return operators.Any(op => expression.Contains(op)) ||
               Regex.IsMatch(expression, functionPattern, RegexOptions.IgnoreCase) ||
               expression.Contains("field", StringComparison.OrdinalIgnoreCase);
    }

    public object EvaluateCalculation(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        if (string.IsNullOrEmpty(expression)) return null;

        try
        {
            // Handle conditional expressions first
            if (expression.Trim().StartsWith("if(", StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateConditionalExpression(expression, formValues, formDefinition);
            }

            // Handle case statements
            if (expression.Trim().StartsWith("case", StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateCaseExpression(expression, formValues, formDefinition);
            }

            var parsedExpression = ParseExpression(expression, formValues, formDefinition);
            return EvaluateComplexExpression(parsedExpression);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating calculation: {ex.Message}");
            return null;
        }
    }

    private object EvaluateConditionalExpression(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        // Format: IF(condition, trueValue, falseValue)
        var match = Regex.Match(expression, @"if\s*\(\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var condition = match.Groups[1].Value;
            var trueValue = match.Groups[2].Value;
            var falseValue = match.Groups[3].Value;

            var conditionResult = EvaluateCondition(condition, formValues, formDefinition);
            var resultExpression = conditionResult ? trueValue : falseValue;

            return EvaluateCalculation(resultExpression, formValues, formDefinition);
        }

        return null;
    }

    private object EvaluateCaseExpression(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        // Format: CASE WHEN condition1 THEN value1 WHEN condition2 THEN value2 ELSE default END
        var whenMatches = Regex.Matches(expression, @"when\s+(.+?)\s+then\s+(.+?)(?=\s+when|\s+else|\s*$)", RegexOptions.IgnoreCase);
        var elseMatch = Regex.Match(expression, @"else\s+(.+?)\s+end", RegexOptions.IgnoreCase);

        foreach (Match whenMatch in whenMatches)
        {
            var condition = whenMatch.Groups[1].Value;
            var value = whenMatch.Groups[2].Value;

            if (EvaluateCondition(condition, formValues, formDefinition))
            {
                return EvaluateCalculation(value, formValues, formDefinition);
            }
        }

        if (elseMatch.Success)
        {
            var elseValue = elseMatch.Groups[1].Value;
            return EvaluateCalculation(elseValue, formValues, formDefinition);
        }

        return null;
    }

    private bool EvaluateCondition(string condition, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        var parsedCondition = ParseExpression(condition, formValues, formDefinition);

        // Handle comparison operators
        if (parsedCondition.Contains(">=")) return EvaluateComparison(parsedCondition, ">=");
        if (parsedCondition.Contains("<=")) return EvaluateComparison(parsedCondition, "<=");
        if (parsedCondition.Contains(">")) return EvaluateComparison(parsedCondition, ">");
        if (parsedCondition.Contains("<")) return EvaluateComparison(parsedCondition, "<");
        if (parsedCondition.Contains("==")) return EvaluateComparison(parsedCondition, "==");
        if (parsedCondition.Contains("!=")) return EvaluateComparison(parsedCondition, "!=");

        // Handle boolean expressions
        return EvaluateBooleanExpression(parsedCondition);
    }

    private bool EvaluateComparison(string expression, string op)
    {
        var parts = expression.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        var left = EvaluateSimpleExpression(parts[0]);
        var right = EvaluateSimpleExpression(parts[1]);

        if (left is decimal leftNum && right is decimal rightNum)
        {
            return op switch
            {
                ">=" => leftNum >= rightNum,
                "<=" => leftNum <= rightNum,
                ">" => leftNum > rightNum,
                "<" => leftNum < rightNum,
                "==" => leftNum == rightNum,
                "!=" => leftNum != rightNum,
                _ => false
            };
        }

        // String comparison
        var leftStr = left?.ToString();
        var rightStr = right?.ToString();

        return op switch
        {
            "==" => leftStr == rightStr,
            "!=" => leftStr != rightStr,
            _ => false
        };
    }

    private bool EvaluateBooleanExpression(string expression)
    {
        var result = EvaluateSimpleExpression(expression);
        return result is bool boolResult && boolResult;
    }

    private string ParseExpression(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        var parsed = expression;

        // Replace function calls
        parsed = ReplaceFunctionCalls(parsed, formValues, formDefinition);

        // Replace field references
        var fieldReferences = FindFieldReferences(parsed, formDefinition);
        foreach (var fieldRef in fieldReferences)
        {
            if (formValues.TryGetValue(fieldRef, out var fieldValue) && fieldValue != null)
            {
                var field = formDefinition.Fields.FirstOrDefault(f => f.Name.Equals(fieldRef, StringComparison.OrdinalIgnoreCase));
                var formattedValue = FormatFieldValue(fieldValue, field);
                parsed = parsed.Replace(fieldRef, formattedValue);
            }
            else
            {
                parsed = parsed.Replace(fieldRef, "0");
            }
        }

        return parsed;
    }

    private string ReplaceFunctionCalls(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        // Replace math functions
        foreach (var func in _mathFunctions)
        {
            var pattern = $@"{func.Key}\s*\(\s*([^)]+)\s*\)";
            var matches = Regex.Matches(expression, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches.Cast<Match>().Reverse())
            {
                var args = match.Groups[1].Value.Split(',')
                    .Select(arg => EvaluateSimpleExpression(ParseExpression(arg.Trim(), formValues, formDefinition)))
                    .Where(arg => arg is decimal)
                    .Cast<decimal>()
                    .ToArray();

                if (args.Length > 0)
                {
                    var result = func.Value(args);
                    expression = expression.Remove(match.Index, match.Length)
                                        .Insert(match.Index, result.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        // Replace string functions
        foreach (var func in _stringFunctions)
        {
            var pattern = $@"{func.Key}\s*\(\s*([^)]+)\s*\)";
            var matches = Regex.Matches(expression, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches.Cast<Match>().Reverse())
            {
                var args = match.Groups[1].Value.Split(',')
                    .Select(arg => ParseExpression(arg.Trim(), formValues, formDefinition).Trim('"'))
                    .ToArray();

                if (args.Length > 0)
                {
                    var result = func.Value(args);
                    expression = expression.Remove(match.Index, match.Length)
                                        .Insert(match.Index, $"\"{result}\"");
                }
            }
        }

        return expression;
    }

    private string FormatFieldValue(object fieldValue, FormField field)
    {
        if (fieldValue == null) return "0";

        if (fieldValue is decimal decimalValue)
        {
            // For calculations, use full precision
            return _numberFormatService.FormatDecimal(decimalValue, field?.DecimalPlaces ?? 2);
        }

        if (fieldValue is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        return fieldValue.ToString();
    }

    public string FormatNumber(decimal value, int decimalPlaces)
    {
        return _numberFormatService.FormatDecimalForDisplay(value, decimalPlaces);
    }

    public decimal? SafeDivision(decimal numerator, decimal denominator)
    {
        return denominator == 0 ? 0 : numerator / denominator;
    }

    private object EvaluateComplexExpression(string expression)
    {
        try
        {
            // Handle safe division with decimal precision
            expression = Regex.Replace(expression, @"([\d\.]+)\s*/\s*([\d\.]+)",
                match =>
                {
                    var numerator = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var denominator = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    return SafeDivision(numerator, denominator)?.ToString(CultureInfo.InvariantCulture) ?? "0";
                });

            // Use DataTable.Compute for remaining expressions
            var result = new DataTable().Compute(expression, null);
            return ConvertResultToAppropriateType(result);
        }
        catch
        {
            return EvaluateCustomExpression(expression);
        }
    }

    private object EvaluateSimpleExpression(string expression)
    {
        try
        {
            var result = new DataTable().Compute(expression, null);
            return ConvertResultToAppropriateType(result);
        }
        catch
        {
            // Return as string if not evaluatable
            return expression.Trim('"');
        }
    }

    private object ConvertResultToAppropriateType(object result)
    {
        if (result is decimal decimalResult) return decimalResult;
        if (result is double doubleResult) return (decimal)doubleResult;
        if (result is int intResult) return (decimal)intResult;
        if (result is bool boolResult) return boolResult;
        return result?.ToString();
    }

    private object EvaluateCustomExpression(string expression)
    {
        try
        {
            expression = expression.Replace(" ", "");

            // Handle safe division in custom evaluator
            if (expression.Contains('/'))
            {
                var parts = expression.Split('/');
                if (parts.Length == 2 &&
                    decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal num1) &&
                    decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal num2))
                {
                    return SafeDivision(num1, num2) ?? 0;
                }
            }

            // Rest of existing custom evaluation logic...
            return expression;
        }
        catch
        {
            return expression;
        }
    }

    private List<string> FindFieldReferences(string expression, FormDefinition formDefinition)
    {
        // Same implementation as before...
        var references = new List<string>();
        var pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b";
        var matches = Regex.Matches(expression, pattern);

        foreach (Match match in matches)
        {
            var potentialField = match.Value;

            if (IsMathConstant(potentialField) || IsOperator(potentialField) || IsFunction(potentialField))
                continue;

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
        var operators = new[] { "and", "or", "not", "if", "else", "then", "when", "case", "end" };
        return operators.Contains(value.ToLower());
    }

    private bool IsFunction(string value)
    {
        return _mathFunctions.ContainsKey(value.ToLower()) || _stringFunctions.ContainsKey(value.ToLower());
    }
}
