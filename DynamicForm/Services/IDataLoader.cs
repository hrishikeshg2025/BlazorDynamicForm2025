using DynamicForm.Models;

namespace DynamicForm.Services;

public interface IDataLoader
{
    Task<List<SelectListItem>> LoadOptionsAsync(DataSourceConfig config, string sourceValue);
}
