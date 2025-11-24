using DynamicForm.Models;

namespace DynamicForm.Services;

public interface ITableLayoutService
{
    FormTable CreateTable(string name, int rows, int columns);
    void MapFieldToCell(FormTable table, string fieldId, int row, int column, int colSpan = 1, int rowSpan = 1);
    void RemoveFieldFromCell(FormTable table, int row, int column);
    TableCell GetCell(FormTable table, int row, int column);
    FormField GetFieldInCell(FormTable table, int row, int column, FormDefinition formDefinition);
    void AddHeader(FormTable table, string text, int columnIndex, int colSpan = 1);
    void AddFooter(FormTable table, string text, int columnIndex, int colSpan = 1);
    void ResizeTable(FormTable table, int newRows, int newColumns);
}
