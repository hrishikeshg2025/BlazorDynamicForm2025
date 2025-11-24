using DynamicForm.Models;

namespace DynamicForm.Services;
public interface IRuleEngineService
{
    Task EvaluateRulesAsync(FormDefinition formDefinition, Dictionary<string, object> formValues, string sourceFieldId);
    Task EvaluateAllRulesAsync(FormDefinition formDefinition, Dictionary<string, object> formValues);
    bool EvaluateCondition(FormField sourceField, object sourceValue, string condition);
}
