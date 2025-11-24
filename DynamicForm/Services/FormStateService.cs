namespace DynamicForm.Services;

public class FormStateService
{
    private readonly Dictionary<string, Dictionary<string, object>> _formStates = new();

    public Dictionary<string, object> GetFormState(string formId)
    {
        if (_formStates.TryGetValue(formId, out var state))
        {
            return new Dictionary<string, object>(state);
        }
        return null;
    }

    public void SaveFormState(string formId, Dictionary<string, object> values)
    {
        _formStates[formId] = new Dictionary<string, object>(values);
    }

    public void ClearFormState(string formId)
    {
        _formStates.Remove(formId);
    }
}
