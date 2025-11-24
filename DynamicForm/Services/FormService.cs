using DynamicForm.Models;

namespace DynamicForm.Services;

public class FormService : IFormService
{
    private static readonly List<FormDefinition> _forms = new();
    private readonly ILogger<FormService> _logger;

    public FormService(ILogger<FormService> logger)
    {
        _logger = logger;
        // Initialize with sample data if needed
    }

    public Task<List<FormDefinition>> GetAllFormsAsync()
    {
        return Task.FromResult(_forms.ToList());
    }

    public Task<FormDefinition> GetFormAsync(string id)
    {
        var form = _forms.FirstOrDefault(f => f.Id == id);
        if (form == null)
        {
            _logger.LogWarning($"Form with ID {id} not found");
        }
        return Task.FromResult(form);
    }

    public Task<string> SaveFormAsync(FormDefinition form)
    {
        if (string.IsNullOrEmpty(form.Id))
        {
            form.Id = Guid.NewGuid().ToString();
            _forms.Add(form);
        }
        else
        {
            var existing = _forms.FirstOrDefault(f => f.Id == form.Id);
            if (existing != null)
            {
                _forms.Remove(existing);
            }
            _forms.Add(form);
        }
        return Task.FromResult(form.Id);
    }

    public Task DeleteFormAsync(string id)
    {
        var form = _forms.FirstOrDefault(f => f.Id == id);
        if (form != null)
        {
            _forms.Remove(form);
        }
        return Task.CompletedTask;
    }
}
