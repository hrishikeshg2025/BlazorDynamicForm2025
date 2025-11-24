using DynamicForm.Models;

namespace DynamicForm.Services;

public interface IEnhancedCalculationEngineService
{
    object EvaluateCalculation(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition);
    bool IsCalculationExpression(string expression);
    string FormatNumber(decimal value, int decimalPlaces);
    decimal? SafeDivision(decimal numerator, decimal denominator);
}
