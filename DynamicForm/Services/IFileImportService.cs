using DynamicForm.Models;
using System.Data;

namespace DynamicForm.Services;

public interface IFileImportService
{
    Task<List<string>> GetFileColumnsAsync(FormDefinition formDefinition, ImportFileType fileType, Stream fileStream);
    Task<DataTable> PreviewFileAsync(FormDefinition formDefinition, ImportFileType fileType, Stream fileStream, int previewRows = 10);
    Task<FileImportResult> ImportDataAsync(FormDefinition formDefinition, string mappingId, Stream fileStream);

    // For saved forms (database mappings)
    Task<List<string>> GetFileColumnsAsync(string formDefinitionId, string mappingId, ImportFileType fileType, Stream fileStream);
    Task<DataTable> PreviewFileAsync(string formDefinitionId, string mappingId, ImportFileType fileType, Stream fileStream, int previewRows = 10);
    Task<FileImportResult> ImportDataAsync(string formDefinitionId, string mappingId, Stream fileStream);

    // Helper methods
    Task<FileImportMapping> GetMappingAsync(FormDefinition formDefinition, string mappingId);
    Task<FileImportMapping> GetMappingAsync(string formDefinitionId, string mappingId);
}
