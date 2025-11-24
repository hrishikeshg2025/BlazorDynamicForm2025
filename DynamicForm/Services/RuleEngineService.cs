using DynamicForm.Models;
using System.Collections.Concurrent;
namespace DynamicForm.Services;
public class RuleEngineService : IRuleEngineService
{
    private readonly ConcurrentDictionary<string, DateTime> _lastEvaluationTimes = new();
    private readonly TimeSpan _evaluationDebounceTime = TimeSpan.FromMilliseconds(100);
    private readonly IEnhancedCalculationEngineService _calculationService;

    public RuleEngineService(IEnhancedCalculationEngineService calculationService)
    {
        _calculationService = calculationService;
    }

    public async Task EvaluateRulesAsync(FormDefinition formDefinition, Dictionary<string, object> formValues, string sourceFieldId)
    {
        // Debounce rule evaluation
        var now = DateTime.UtcNow;
        var lastEvaluation = _lastEvaluationTimes.GetValueOrDefault(sourceFieldId, DateTime.MinValue);

        if (now - lastEvaluation < _evaluationDebounceTime)
        {
            return;
        }

        _lastEvaluationTimes[sourceFieldId] = now;

        var sourceField = formDefinition.Fields.FirstOrDefault(f => f.Id == sourceFieldId);
        if (sourceField == null) return;

        foreach (var rule in sourceField.Rules)
        {
            await ProcessRuleAsync(formDefinition, formValues, sourceField, rule);
        }
    }

    public async Task EvaluateAllRulesAsync(FormDefinition formDefinition, Dictionary<string, object> formValues)
    {
        foreach (var field in formDefinition.Fields)
        {
            await EvaluateRulesAsync(formDefinition, formValues, field.Id);
        }
    }

    private async Task ProcessRuleAsync(FormDefinition formDefinition, Dictionary<string, object> formValues, FormField sourceField, FieldRule rule)
    {
        var targetField = formDefinition.Fields.FirstOrDefault(f => f.Id == rule.TargetFieldId);
        if (targetField == null) return;

        var sourceValue = formValues.TryGetValue(sourceField.Name, out var val) ? val?.ToString() : null;
        var conditionMet = string.IsNullOrEmpty(rule.Condition) ||
                            EvaluateCondition(sourceField, sourceValue, rule.Condition);

        if (conditionMet)
        {
            await ApplyActionsAsync(formValues, targetField, rule.Actions, sourceField, sourceValue, formDefinition);
        }
        else
        {
            RevertToOriginalState(targetField, formValues);
        }
    }

    private async Task ApplyActionsAsync(Dictionary<string, object> formValues, FormField targetField,
                                        List<FieldAction> actions, FormField sourceField, string sourceValue,
                                        FormDefinition formDefinition)
    {
        foreach (var action in actions)
        {
            switch (action.Type.ToLower())
            {
                case "show":
                    targetField.IsHidden = false;
                    break;
                case "hide":
                    targetField.IsHidden = true;
                    break;
                case "enable":
                    targetField.IsReadonly = false;
                    break;
                case "disable":
                    targetField.IsReadonly = true;
                    break;
                case "setvalue":
                    formValues[targetField.Name] = action.Value;
                    break;
                case "calculate":
                    await HandleCalculationAsync(action, formValues, targetField, formDefinition);
                    break;
                case "setrequired":
                    targetField.IsRequired = Convert.ToBoolean(action.Value);
                    break;
                case "loadoptions":
                    if (targetField.Type == FieldType.DropDown || targetField.Type == FieldType.CascadingDropDown)
                    {
                        await UpdateDropdownOptionsAsync(sourceField, targetField, sourceValue);
                    }
                    break;
            }
        }
    }
    private async Task HandleCalculationAsync(FieldAction action, Dictionary<string, object> formValues,
                                            FormField targetField, FormDefinition formDefinition)
    {
        if (string.IsNullOrEmpty(action.Expression))
            return;

        // Evaluate the calculation expression
        var result = _calculationService.EvaluateCalculation(action.Expression, formValues, formDefinition);

        if (result != null)
        {
            // Format the result if it's a number
            if (result is decimal decimalResult && targetField.Type == FieldType.Number)
            {
                result = _calculationService.FormatNumber(decimalResult, targetField.DecimalPlaces ?? 2);
            }

            // Set the result to the target field
            formValues[targetField.Name] = result;

            // If the target field has rules, evaluate them too
            if (targetField.Rules.Any())
            {
                await EvaluateRulesAsync(formDefinition, formValues, targetField.Id);
            }            
        }
    }
    private void RevertToOriginalState(FormField targetField, Dictionary<string, object> formValues)
    {
        targetField.IsHidden = targetField.OriginalIsHidden;
        targetField.IsReadonly = targetField.OriginalIsReadonly;
        targetField.IsRequired = targetField.OriginalIsRequired;
        formValues[targetField.Name] = targetField.OriginalValue;
    }

    public bool EvaluateCondition(FormField sourceField, object sourceValue, string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;

        return sourceField.Type switch
        {
            FieldType.Number when int.TryParse(sourceValue?.ToString(), out var numValue) =>
                EvaluateNumericCondition(numValue, condition),
            FieldType.Checkbox when bool.TryParse(sourceValue?.ToString(), out var boolValue) =>
                EvaluateBooleanCondition(boolValue, condition),
            _ => EvaluateStringCondition(sourceValue?.ToString(), condition)
        };
    }

    private bool EvaluateNumericCondition(int value, string condition)
    {
        if (condition.Contains(">=")) return value >= ParseNumber(condition.Split(">=")[1]);
        if (condition.Contains("<=")) return value <= ParseNumber(condition.Split("<=")[1]);
        if (condition.Contains(">")) return value > ParseNumber(condition.Split(">")[1]);
        if (condition.Contains("<")) return value < ParseNumber(condition.Split("<")[1]);
        if (condition.Contains("==")) return value == ParseNumber(condition.Split("==")[1]);
        if (condition.Contains("!=")) return value != ParseNumber(condition.Split("!=")[1]);
        return false;
    }

    private bool EvaluateBooleanCondition(bool value, string condition)
    {
        return condition.ToLower() switch
        {
            "true" => value,
            "false" => !value,
            _ => false
        };
    }

    private bool EvaluateStringCondition(string value, string condition)
    {
        if (string.IsNullOrEmpty(value)) return false;

        if (condition.StartsWith("==")) return value == condition.Substring(2).Trim();
        if (condition.StartsWith("!=")) return value != condition.Substring(2).Trim();
        if (condition.StartsWith("contains ")) return value.Contains(condition.Substring(9).Trim());
        if (condition.StartsWith("startswith ")) return value.StartsWith(condition.Substring(11).Trim());
        if (condition.StartsWith("endswith ")) return value.EndsWith(condition.Substring(9).Trim());

        return false;
    }

    private int ParseNumber(string value) => int.TryParse(value.Trim(), out var num) ? num : 0;

    private async Task UpdateDropdownOptionsAsync(FormField sourceField, FormField targetField, string sourceValue)
    {
        if (targetField.CascadingData != null &&
            targetField.CascadingData.TryGetValue(sourceValue, out var options))
        {
            targetField.Data = options;
        }
        else
        {
            targetField.Data = new List<SelectListItem>();
        }
    }
}

