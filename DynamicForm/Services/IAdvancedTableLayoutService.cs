using DynamicForm.Models;

namespace DynamicForm.Services;

public interface IAdvancedTableLayoutService
{
    // Table Management
    DynamicTable CreateTable(string name, int rows = 1, int columns = 1);
    void ResizeTable(DynamicTable table, int newRows, int newColumns);
    void AddRow(DynamicTable table, int atIndex = -1);
    void RemoveRow(DynamicTable table, int rowIndex);
    void AddColumn(DynamicTable table, int atIndex = -1);
    void RemoveColumn(DynamicTable table, int columnIndex);

    // Cell Management
    void MapFieldToCell(DynamicTable table, string fieldId, int row, int column,
                       int colSpan = 1, int rowSpan = 1, string label = null);
    void SetCellText(DynamicTable table, int row, int column, string text, int colSpan = 1, int rowSpan = 1);
    void SetCellCalculation(DynamicTable table, int row, int column, string expression, int colSpan = 1, int rowSpan = 1);
    void AddNestedTable(DynamicTable table, int row, int column, DynamicTable nestedTable);
    void RemoveNestedTable(DynamicTable table, int row, int column);
    void ClearCell(DynamicTable table, int row, int column);

    // Cell Span Management
    void SetCellSpan(DynamicTable table, int row, int column, int colSpan, int rowSpan);
    void MergeCells(DynamicTable table, int startRow, int startColumn, int endRow, int endColumn);
    void SplitCell(DynamicTable table, int row, int column);

    // Header/Footer Management
    void SetHeader(DynamicTable table, int column, string text, int colSpan = 1);
    void SetFooter(DynamicTable table, int column, string text, int colSpan = 1);

    // Data Access
    TableCell GetCell(DynamicTable table, int row, int column);
    FormField GetFieldInCell(DynamicTable table, int row, int column, FormDefinition formDefinition);
    object GetCellValue(DynamicTable table, int row, int column, Dictionary<string, object> formValues, FormDefinition formDefinition);

    // Calculation Support
    List<TableReference> ParseCellReferences(string expression);
    string EvaluateCellCalculation(string expression, Dictionary<string, object> allValues, List<DynamicTable> allTables, FormDefinition formDefinition);
    void RecalculateTable(DynamicTable table, Dictionary<string, object> formValues, FormDefinition formDefinition, List<DynamicTable> allTables);
}
