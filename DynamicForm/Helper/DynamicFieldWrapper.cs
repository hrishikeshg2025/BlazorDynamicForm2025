using DynamicForm.Models;

namespace DynamicForm.Helper;

public class DynamicFieldWrapper
{
    private readonly Dictionary<string, object> _values;
    private readonly string _fieldName;

    public DynamicFieldWrapper(Dictionary<string, object> values, string fieldName)
    {
        _values = values;
        _fieldName = fieldName;
        Options = new List<SelectListItem>();
    }

    public string Value
    {
        get => _values.TryGetValue(_fieldName, out var value) ? value?.ToString() : null;
        set => _values[_fieldName] = value;
    }

    // Number value
    public int? NumberValue
    {
        get => _values.TryGetValue(_fieldName, out var value) && int.TryParse(value?.ToString(), out var num) ? num : null;
        set => _values[_fieldName] = value;
    }

    // Boolean value
    public bool BooleanValue
    {
        get => _values.TryGetValue(_fieldName, out var value) && bool.TryParse(value?.ToString(), out var boolVal) && boolVal;
        set => _values[_fieldName] = value;
    }

    // Date value
    public DateTime? DateValue
    {
        get => _values.TryGetValue(_fieldName, out var value) && DateTime.TryParse(value?.ToString(), out var date) ? date : null;
        set => _values[_fieldName] = value;
    }
    public List<SelectListItem> Options { get; set; } = new();
}
