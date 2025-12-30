namespace DynamicForm.Models;
// Add to DynamicForm.Models namespace
public class FileImportMapping
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public ImportFileType FileType { get; set; }
    public List<ColumnMapping> ColumnMappings { get; set; } = new();
    public string FormDefinitionId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedDate { get; set; }
}

public enum ImportFileType
{
    CSV,
    Excel
}

public class ColumnMapping
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileColumnName { get; set; }
    public string FileColumnIndex { get; set; } // Can be column letter (A, B, C) or index
    public string FieldId { get; set; } // References FormField.Id
}

public class ValidationRule
{
    public string Type { get; set; } // "regex", "range", "list", "custom"
    public string Pattern { get; set; }
    public string ErrorMessage { get; set; }
}

public class FileImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int RowsProcessed { get; set; }
    public int RowsImported { get; set; }
    public int RowsFailed { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public Dictionary<string, object> ImportedData { get; set; } = new();
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string Column { get; set; }
    public string Error { get; set; }
    public string Value { get; set; }
}