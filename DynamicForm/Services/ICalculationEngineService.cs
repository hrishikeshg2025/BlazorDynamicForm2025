using DynamicForm.Models;

namespace DynamicForm.Services;

public interface ICalculationEngineService
{
    object EvaluateCalculation(string expression, Dictionary<string, object> formValues, FormDefinition formDefinition);
    bool IsCalculationExpression(string expression);
}

