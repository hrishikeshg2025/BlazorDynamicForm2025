using DynamicForm.Models;

namespace DynamicForm.Services;

public interface IFormService
{
    Task<List<FormDefinition>> GetAllFormsAsync();
    Task<FormDefinition> GetFormAsync(string id);
    Task<string> SaveFormAsync(FormDefinition form); // Returns the ID
    Task DeleteFormAsync(string id);
}
