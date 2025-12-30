namespace DynamicForm.Services;

using CsvHelper;
using CsvHelper.Configuration;
// Services/FileImportService.cs
using DynamicForm.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.Globalization;

public class FileImportService : IFileImportService
{
    private readonly IFormService _formService;
    private readonly ILogger<FileImportService> _logger;

    public FileImportService(IFormService formService, ILogger<FileImportService> logger)
    {
        _formService = formService;
        _logger = logger;
    }

    // ==================== FOR FORM BUILDER (In-Memory Mappings) ====================
    
    public async Task<List<string>> GetFileColumnsAsync(FormDefinition formDefinition, ImportFileType fileType, Stream fileStream)
    {
        return await GetFileColumnsInternalAsync(fileType, fileStream);
    }

    public async Task<DataTable> PreviewFileAsync(FormDefinition formDefinition, ImportFileType fileType, Stream fileStream, int previewRows = 10)
    {
        return await PreviewFileInternalAsync(fileType, fileStream, previewRows);
    }

    public async Task<FileImportResult> ImportDataAsync(FormDefinition formDefinition, string mappingId, Stream fileStream)
    {
        // Get mapping from in-memory form definition
        var mapping = GetMappingFromFormDefinition(formDefinition, mappingId);
        if (mapping == null)
        {
            return new FileImportResult
            {
                Success = false,
                Message = $"Mapping with ID '{mappingId}' not found in form definition"
            };
        }

        return await ImportDataInternalAsync(formDefinition, mapping, fileStream);
    }

    // ==================== FOR SAVED FORMS (Database Mappings) ====================

    public async Task<List<string>> GetFileColumnsAsync(string formDefinitionId, string mappingId, ImportFileType fileType, Stream fileStream)
    {
        // Verify mapping exists
        var mapping = await GetMappingAsync(formDefinitionId, mappingId);
        if (mapping == null)
        {
            _logger.LogWarning($"Mapping {mappingId} not found for form {formDefinitionId}");
            return new List<string>();
        }

        return await GetFileColumnsInternalAsync(fileType, fileStream);
    }

    public async Task<DataTable> PreviewFileAsync(string formDefinitionId, string mappingId, ImportFileType fileType, Stream fileStream, int previewRows = 10)
    {
        // Verify mapping exists
        var mapping = await GetMappingAsync(formDefinitionId, mappingId);
        if (mapping == null)
        {
            _logger.LogWarning($"Mapping {mappingId} not found for form {formDefinitionId}");
            return new DataTable();
        }

        return await PreviewFileInternalAsync(fileType, fileStream, previewRows);
    }

    public async Task<FileImportResult> ImportDataAsync(string formDefinitionId, string mappingId, Stream fileStream)
    {
        // Load form and mapping from database
        var formDefinition = await _formService.GetFormAsync(formDefinitionId);
        if (formDefinition == null)
        {
            return new FileImportResult
            {
                Success = false,
                Message = $"Form with ID '{formDefinitionId}' not found"
            };
        }

        var mapping = await GetMappingAsync(formDefinitionId, mappingId);
        if (mapping == null)
        {
            return new FileImportResult
            {
                Success = false,
                Message = $"Mapping with ID '{mappingId}' not found"
            };
        }

        return await ImportDataInternalAsync(formDefinition, mapping, fileStream);
    }

    // ==================== HELPER METHODS ====================

    public async Task<FileImportMapping> GetMappingAsync(FormDefinition formDefinition, string mappingId)
    {
        return GetMappingFromFormDefinition(formDefinition, mappingId);
    }

    public async Task<FileImportMapping> GetMappingAsync(string formDefinitionId, string mappingId)
    {
        // First try to get from database (you'll need to implement this in IFormService)
        // For now, we'll load the form and get mapping from it
        var formDefinition = await _formService.GetFormAsync(formDefinitionId);
        return GetMappingFromFormDefinition(formDefinition, mappingId);
    }

    // ==================== PRIVATE IMPLEMENTATION METHODS ====================

    private FileImportMapping GetMappingFromFormDefinition(FormDefinition formDefinition, string mappingId)
    {
        if (formDefinition == null || formDefinition.ImportMappings == null)
            return null;

        return formDefinition.ImportMappings.FirstOrDefault(m => m.Id == mappingId);
    }

    private async Task<List<string>> GetFileColumnsInternalAsync(ImportFileType fileType, Stream fileStream)
    {
        var columns = new List<string>();

        try
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            switch (fileType)
            {
                case ImportFileType.CSV:
                    using (var reader = new StreamReader(memoryStream, leaveOpen: true))
                    {
                        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            PrepareHeaderForMatch = args => args.Header.ToLower(),
                        };

                        using (var csv = new CsvReader(reader, config))
                        {
                            if (await csv.ReadAsync())
                            {
                                csv.ReadHeader();
                                if (csv.HeaderRecord != null)
                                {
                                    columns.AddRange(csv.HeaderRecord);
                                }
                            }
                        }
                    }
                    break;

                case ImportFileType.Excel:
                    IWorkbook workbook;

                    // Detect format
                    var header = new byte[8];
                    memoryStream.Read(header, 0, 8);
                    memoryStream.Position = 0;

                    if (header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
                    {
                        workbook = new HSSFWorkbook(memoryStream);
                    }
                    else
                    {
                        workbook = new XSSFWorkbook(memoryStream);
                    }

                    var sheet = workbook.GetSheetAt(0);
                    var headerRow = sheet.GetRow(0);

                    if (headerRow != null)
                    {
                        for (int i = 0; i < headerRow.LastCellNum; i++)
                        {
                            var cell = headerRow.GetCell(i);
                            columns.Add(cell?.ToString()?.Trim() ?? $"Column{i + 1}");
                        }
                    }

                    workbook.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file columns");
        }

        return columns;
    }

    private async Task<DataTable> PreviewFileInternalAsync(ImportFileType fileType, Stream fileStream, int previewRows)
    {
        var dataTable = new DataTable();

        try
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            switch (fileType)
            {
                case ImportFileType.CSV:
                    using (var reader = new StreamReader(memoryStream, leaveOpen: true))
                    {
                        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            PrepareHeaderForMatch = args => args.Header.ToLower(),
                        };

                        using (var csv = new CsvReader(reader, config))
                        {
                            if (await csv.ReadAsync())
                            {
                                csv.ReadHeader();

                                if (csv.HeaderRecord != null)
                                {
                                    // Create columns
                                    foreach (var rheader in csv.HeaderRecord)
                                    {
                                        dataTable.Columns.Add(rheader);
                                    }

                                    // Read data rows
                                    int rowCount = 0;
                                    while (await csv.ReadAsync() && rowCount < previewRows)
                                    {
                                        var row = dataTable.NewRow();
                                        foreach (DataColumn column in dataTable.Columns)
                                        {
                                            try
                                            {
                                                row[column.ColumnName] = csv.GetField(column.ColumnName)?.ToString() ?? string.Empty;
                                            }
                                            catch
                                            {
                                                row[column.ColumnName] = string.Empty;
                                            }
                                        }
                                        dataTable.Rows.Add(row);
                                        rowCount++;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case ImportFileType.Excel:
                    IWorkbook workbook;

                    // Detect format
                    var header = new byte[8];
                    memoryStream.Read(header, 0, 8);
                    memoryStream.Position = 0;

                    if (header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
                    {
                        workbook = new HSSFWorkbook(memoryStream);
                    }
                    else
                    {
                        workbook = new XSSFWorkbook(memoryStream);
                    }

                    var sheet = workbook.GetSheetAt(0);
                    var headerRow = sheet.GetRow(0);

                    if (headerRow != null)
                    {
                        // Create columns
                        for (int col = 0; col < headerRow.LastCellNum; col++)
                        {
                            var cell = headerRow.GetCell(col);
                            dataTable.Columns.Add(cell?.ToString()?.Trim() ?? $"Column{col + 1}");
                        }

                        // Create rows
                        for (int rowNum = 1; rowNum <= Math.Min(sheet.LastRowNum, previewRows); rowNum++)
                        {
                            var row = sheet.GetRow(rowNum);
                            if (row != null)
                            {
                                var dataRow = dataTable.NewRow();
                                for (int col = 0; col < dataTable.Columns.Count; col++)
                                {
                                    var cell = row.GetCell(col);
                                    dataRow[col] = cell?.ToString()?.Trim() ?? string.Empty;
                                }
                                dataTable.Rows.Add(dataRow);
                            }
                        }
                    }

                    workbook.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing file");
        }

        return dataTable;
    }

    private async Task<FileImportResult> ImportDataInternalAsync(FormDefinition formDefinition, FileImportMapping mapping, Stream fileStream)
    {
        var result = new FileImportResult();

        try
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var importData = new Dictionary<string, object>();

            switch (mapping.FileType)
            {
                case ImportFileType.CSV:
                    result = await ImportCsvAsync(formDefinition, mapping, memoryStream);
                    break;

                case ImportFileType.Excel:
                    result = await ImportExcelAsync(formDefinition, mapping, memoryStream);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing file");
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
        }

        return result;
    }

    private async Task<FileImportResult> ImportCsvAsync(FormDefinition formDefinition, FileImportMapping mapping, MemoryStream memoryStream)
    {
        var result = new FileImportResult();
        var importData = new Dictionary<string, object>();

        try
        {
            using var reader = new StreamReader(memoryStream, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            using var csv = new CsvReader(reader, config);

            if (await csv.ReadAsync())
            {
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                // Process only first row (for form preview)
                if (await csv.ReadAsync())
                {
                    result.RowsProcessed = 1;

                    try
                    {
                        foreach (var columnMapping in mapping.ColumnMappings)
                        {
                            var field = formDefinition.Fields.FirstOrDefault(f => f.Id == columnMapping.FieldId);
                            if (field == null) continue;

                            // Find matching column
                            string columnValue = string.Empty;
                            var matchingHeader = headers.FirstOrDefault(h =>
                                h.Equals(columnMapping.FileColumnName, StringComparison.OrdinalIgnoreCase));

                            if (matchingHeader != null)
                            {
                                columnValue = csv.GetField(matchingHeader)?.ToString() ?? string.Empty;
                            }

                            // Apply field default if empty
                            if (string.IsNullOrEmpty(columnValue) && !string.IsNullOrEmpty(field.DefaultValue))
                            {
                                columnValue = field.DefaultValue;
                            }

                            // Validate required fields
                            if (field.IsRequired && string.IsNullOrEmpty(columnValue))
                            {
                                result.Errors.Add(new ImportError
                                {
                                    RowNumber = 1,
                                    Column = columnMapping.FileColumnName,
                                    Error = "Required field is empty",
                                    Value = columnValue
                                });
                                continue;
                            }

                            // Convert to appropriate type
                            object typedValue = ConvertToFieldType(columnValue, field.Type);
                            importData[field.Name] = typedValue;
                        }

                        if (!result.Errors.Any())
                        {
                            result.RowsImported = 1;
                            result.ImportedData = importData;
                            result.Success = true;
                            result.Message = "Data imported successfully";
                        }
                        else
                        {
                            result.RowsFailed = 1;
                            result.Success = false;
                            result.Message = "Import completed with errors";
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = 1,
                            Column = "General",
                            Error = ex.Message,
                            Value = "Row processing error"
                        });
                        result.RowsFailed = 1;
                        result.Success = false;
                        result.Message = $"Import failed: {ex.Message}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV");
            result.Success = false;
            result.Message = $"CSV import failed: {ex.Message}";
        }

        return result;
    }

    private async Task<FileImportResult> ImportExcelAsync(FormDefinition formDefinition, FileImportMapping mapping, MemoryStream memoryStream)
    {
        var result = new FileImportResult();
        var importData = new Dictionary<string, object>();

        try
        {
            IWorkbook workbook;

            // Detect format
            var header = new byte[8];
            memoryStream.Read(header, 0, 8);
            memoryStream.Position = 0;

            if (header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0)
            {
                workbook = new HSSFWorkbook(memoryStream);
            }
            else
            {
                workbook = new XSSFWorkbook(memoryStream);
            }

            var sheet = workbook.GetSheetAt(0);

            // Process only first data row (row 1, skipping header at row 0)
            if (sheet.LastRowNum >= 1)
            {
                result.RowsProcessed = 1;
                var dataRow = sheet.GetRow(1); // First data row

                if (dataRow != null)
                {
                    try
                    {
                        foreach (var columnMapping in mapping.ColumnMappings)
                        {
                            var field = formDefinition.Fields.FirstOrDefault(f => f.Id == columnMapping.FieldId);
                            if (field == null) continue;

                            // Find column index
                            int columnIndex = -1;
                            var headerRow = sheet.GetRow(0);
                            if (headerRow != null)
                            {
                                for (int col = 0; col < headerRow.LastCellNum; col++)
                                {
                                    var cell = headerRow.GetCell(col);
                                    if (cell != null &&
                                        cell.ToString().Equals(columnMapping.FileColumnName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        columnIndex = col;
                                        break;
                                    }
                                }
                            }

                            string columnValue = string.Empty;
                            if (columnIndex >= 0)
                            {
                                var cell = dataRow.GetCell(columnIndex);
                                columnValue = cell?.ToString()?.Trim() ?? string.Empty;
                            }

                            // Apply field default if empty
                            if (string.IsNullOrEmpty(columnValue) && !string.IsNullOrEmpty(field.DefaultValue))
                            {
                                columnValue = field.DefaultValue;
                            }

                            // Validate required fields
                            if (field.IsRequired && string.IsNullOrEmpty(columnValue))
                            {
                                result.Errors.Add(new ImportError
                                {
                                    RowNumber = 1,
                                    Column = columnMapping.FileColumnName,
                                    Error = "Required field is empty",
                                    Value = columnValue
                                });
                                continue;
                            }

                            // Convert to appropriate type
                            object typedValue = ConvertToFieldType(columnValue, field.Type);
                            importData[field.Name] = typedValue;
                        }

                        if (!result.Errors.Any())
                        {
                            result.RowsImported = 1;
                            result.ImportedData = importData;
                            result.Success = true;
                            result.Message = "Data imported successfully";
                        }
                        else
                        {
                            result.RowsFailed = 1;
                            result.Success = false;
                            result.Message = "Import completed with errors";
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = 1,
                            Column = "General",
                            Error = ex.Message,
                            Value = "Row processing error"
                        });
                        result.RowsFailed = 1;
                        result.Success = false;
                        result.Message = $"Import failed: {ex.Message}";
                    }
                }
            }

            workbook.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Excel");
            result.Success = false;
            result.Message = $"Excel import failed: {ex.Message}";
        }

        return result;
    }

    private object ConvertToFieldType(string value, FieldType fieldType)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return fieldType switch
            {
                FieldType.Number => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal num) ? num : 0,
                FieldType.Checkbox => bool.TryParse(value, out bool boolVal) ? boolVal :
                                     value.ToLower() == "true" || value == "1" || value.ToLower() == "yes",
                FieldType.Date => DateTime.TryParse(value, out DateTime date) ? date : (object)null,
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }
}
