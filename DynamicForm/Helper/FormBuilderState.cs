using DynamicForm.Models;

namespace DynamicForm.Helper;

public class FormBuilderState
{
    public FormDefinition CurrentForm { get; private set; } = new();
    public bool ShowPreview { get; private set; }
    public Type PreviewModelType { get; set; } = typeof(Dictionary<string, object>);

    public event Action OnChange;

    public void NewForm()
    {
        CurrentForm = new FormDefinition
        {
            Name = "New Form",
            Fields = new List<FormField>()
        };
        CurrentForm.InitializeDefaultLayout();
        NotifyStateChanged();
    }

    public void LoadForm(FormDefinition form)
    {
        CurrentForm = form;
        CurrentForm.InitializeDefaultLayout();
        NotifyStateChanged();
    }

    public void TogglePreview()
    {
        ShowPreview = !ShowPreview;
        NotifyStateChanged();
    }

    public void SetPreviewModelType(Type type)
    {
        PreviewModelType = type;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}
