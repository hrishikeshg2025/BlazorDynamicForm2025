using DynamicForm.Models;

namespace DynamicForm.Services;

public class TableLayoutService : ITableLayoutService
{
    public FormTable CreateTable(string name, int rows, int columns)
    {
        var table = new FormTable
        {
            Name = name,
            Rows = rows,
            Columns = columns,
            TableRows = new List<TableRow>()
        };

        // Initialize rows and cells
        for (int i = 0; i < rows; i++)
        {
            var row = new TableRow { RowIndex = i, Cells = new List<TableCell>() };
            for (int j = 0; j < columns; j++)
            {
                row.Cells.Add(new TableCell
                {
                    RowIndex = i,
                    ColumnIndex = j,
                    ColSpan = 1,
                    RowSpan = 1
                });
            }
            table.TableRows.Add(row);
        }

        // Initialize headers
        for (int j = 0; j < columns; j++)
        {
            table.Headers.Add(new TableHeader
            {
                Text = $"Header {j + 1}",
                ColSpan = 1
            });
        }

        return table;
    }

    public void MapFieldToCell(FormTable table, string fieldId, int row, int column, int colSpan = 1, int rowSpan = 1)
    {
        if (row < 0 || row >= table.Rows || column < 0 || column >= table.Columns)
            throw new ArgumentException("Invalid row or column index");

        var cell = GetCell(table, row, column);
        cell.FieldId = fieldId;
        cell.ColSpan = colSpan;
        cell.RowSpan = rowSpan;

        // Clear overlapping cells
        for (int r = row; r < row + rowSpan; r++)
        {
            for (int c = column; c < column + colSpan; c++)
            {
                if (r == row && c == column) continue; // Skip the main cell
                var overlappingCell = GetCell(table, r, c);
                overlappingCell.FieldId = null;
                overlappingCell.ColSpan = 1;
                overlappingCell.RowSpan = 1;
            }
        }
    }

    public void RemoveFieldFromCell(FormTable table, int row, int column)
    {
        var cell = GetCell(table, row, column);
        cell.FieldId = null;
        cell.ColSpan = 1;
        cell.RowSpan = 1;
    }

    public TableCell GetCell(FormTable table, int row, int column)
    {
        if (row < table.TableRows.Count && column < table.TableRows[row].Cells.Count)
        {
            return table.TableRows[row].Cells[column];
        }
        return null;
    }

    public FormField GetFieldInCell(FormTable table, int row, int column, FormDefinition formDefinition)
    {
        var cell = GetCell(table, row, column);
        if (cell != null && !string.IsNullOrEmpty(cell.FieldId))
        {
            return formDefinition.Fields.FirstOrDefault(f => f.Id == cell.FieldId);
        }
        return null;
    }

    public void AddHeader(FormTable table, string text, int columnIndex, int colSpan = 1)
    {
        if (columnIndex < table.Headers.Count)
        {
            table.Headers[columnIndex].Text = text;
            table.Headers[columnIndex].ColSpan = colSpan;
        }
    }

    public void AddFooter(FormTable table, string text, int columnIndex, int colSpan = 1)
    {
        if (table.Footer == null)
        {
            table.Footer = new TableFooter();
        }

        // Ensure we have enough footer cells
        while (table.Footer.Cells.Count <= columnIndex)
        {
            table.Footer.Cells.Add(new TableCell());
        }

        table.Footer.Cells[columnIndex].Text = text;
        table.Footer.Cells[columnIndex].ColSpan = colSpan;
    }

    public void ResizeTable(FormTable table, int newRows, int newColumns)
    {
        // Add or remove rows
        while (table.TableRows.Count < newRows)
        {
            var newRowIndex = table.TableRows.Count;
            var row = new TableRow { RowIndex = newRowIndex, Cells = new List<TableCell>() };
            for (int j = 0; j < newColumns; j++)
            {
                row.Cells.Add(new TableCell
                {
                    RowIndex = newRowIndex,
                    ColumnIndex = j,
                    ColSpan = 1,
                    RowSpan = 1
                });
            }
            table.TableRows.Add(row);
        }

        while (table.TableRows.Count > newRows)
        {
            table.TableRows.RemoveAt(table.TableRows.Count - 1);
        }

        // Adjust columns in each row
        foreach (var row in table.TableRows)
        {
            while (row.Cells.Count < newColumns)
            {
                row.Cells.Add(new TableCell
                {
                    RowIndex = row.RowIndex,
                    ColumnIndex = row.Cells.Count,
                    ColSpan = 1,
                    RowSpan = 1
                });
            }

            while (row.Cells.Count > newColumns)
            {
                row.Cells.RemoveAt(row.Cells.Count - 1);
            }
        }

        // Adjust headers
        while (table.Headers.Count < newColumns)
        {
            table.Headers.Add(new TableHeader
            {
                Text = $"Header {table.Headers.Count + 1}",
                ColSpan = 1
            });
        }

        while (table.Headers.Count > newColumns)
        {
            table.Headers.RemoveAt(table.Headers.Count - 1);
        }

        table.Rows = newRows;
        table.Columns = newColumns;
    }
}