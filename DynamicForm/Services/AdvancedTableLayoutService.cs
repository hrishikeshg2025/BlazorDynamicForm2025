using DynamicForm.Models;

namespace DynamicForm.Services;

public class AdvancedTableLayoutService : IAdvancedTableLayoutService
{
    private readonly IEnhancedCalculationEngineService _calculationService;

    public AdvancedTableLayoutService(IEnhancedCalculationEngineService calculationService)
    {
        _calculationService = calculationService;
    }

    public DynamicTable CreateTable(string name, int rows = 1, int columns = 1)
    {              
        var table = new DynamicTable
        {
            Name = name,
            Rows = Math.Max(1, rows),
            Columns = Math.Max(1, columns),
            TableRows = new List<TableRow>(),
            Headers = new List<TableHeader>(),
            Footer = new TableFooter { Cells = new List<TableCell>() }
        };

        InitializeTableStructure(table);
        return table;        
    }

    private void InitializeTableStructure(DynamicTable table)
    {
        // Ensure collections are initialized
        table.TableRows ??= new List<TableRow>();
        table.Headers ??= new List<TableHeader>();
        table.Footer ??= new TableFooter { Cells = new List<TableCell>() };
        table.Footer.Cells ??= new List<TableCell>();

        table.TableRows.Clear();
        table.Headers.Clear();
        table.Footer.Cells.Clear();

        // Initialize rows and cells
        for (int i = 0; i < table.Rows; i++)
        {
            var row = new TableRow
            {
                RowIndex = i,
                Cells = new List<TableCell>()
            };

            for (int j = 0; j < table.Columns; j++)
            {
                row.Cells.Add(new TableCell
                {
                    RowIndex = i,
                    ColumnIndex = j,
                    ColSpan = 1,
                    RowSpan = 1,
                    CellType = CellType.StaticText,
                    Text = string.Empty // Initialize with empty text
                });
            }
            table.TableRows.Add(row);
        }

        // Initialize headers 
        for (int j = 0; j < table.Columns; j++)
        {
            table.Headers.Add(new TableHeader
            {
                Text = $"Column {j + 1}",
                ColSpan = 1
            });
        }

        // Initialize footer - SAFE VERSION  
        for (int j = 0; j < table.Columns; j++)
        {
            table.Footer.Cells.Add(new TableCell
            {
                ColumnIndex = j,
                ColSpan = 1,
                Text = string.Empty
            });
        }
    }

    public void ResizeTable(DynamicTable table, int newRows, int newColumns)
    {
        newRows = Math.Max(1, newRows);
        newColumns = Math.Max(1, newColumns);

        // Handle row changes
        while (table.TableRows.Count < newRows)
        {
            AddRow(table, table.TableRows.Count);
        }
        while (table.TableRows.Count > newRows)
        {
            RemoveRow(table, table.TableRows.Count - 1);
        }

        // Handle column changes in each row
        foreach (var row in table.TableRows)
        {
            while (row.Cells.Count < newColumns)
            {
                AddColumnToRow(row, row.Cells.Count);
            }
            while (row.Cells.Count > newColumns)
            {
                RemoveColumnFromRow(row, row.Cells.Count - 1);
            }
        }

        // Update headers and footer
        ResizeHeaders(table, newColumns);
        ResizeFooter(table, newColumns);

        table.Rows = newRows;
        table.Columns = newColumns;
    }

    public void AddRow(DynamicTable table, int atIndex = -1)
    {
        if (atIndex < 0) atIndex = table.TableRows.Count;
        atIndex = Math.Min(atIndex, table.TableRows.Count);

        var newRow = new TableRow { RowIndex = atIndex, Cells = new List<TableCell>() };
        for (int j = 0; j < table.Columns; j++)
        {
            newRow.Cells.Add(new TableCell
            {
                RowIndex = atIndex,
                ColumnIndex = j,
                ColSpan = 1,
                RowSpan = 1,
                CellType = CellType.StaticText
            });
        }

        table.TableRows.Insert(atIndex, newRow);

        // Update row indices
        for (int i = atIndex + 1; i < table.TableRows.Count; i++)
        {
            table.TableRows[i].RowIndex = i;
        }

        table.Rows++;
    }

    public void RemoveRow(DynamicTable table, int rowIndex)
    {
        if (table.Rows <= 1) return; // Keep at least one row

        if (rowIndex >= 0 && rowIndex < table.TableRows.Count)
        {
            table.TableRows.RemoveAt(rowIndex);

            // Update row indices
            for (int i = rowIndex; i < table.TableRows.Count; i++)
            {
                table.TableRows[i].RowIndex = i;
            }

            table.Rows--;
        }
    }

    public void AddColumn(DynamicTable table, int atIndex = -1)
    {
        if (atIndex < 0) atIndex = table.Columns;
        atIndex = Math.Min(atIndex, table.Columns);

        foreach (var row in table.TableRows)
        {
            AddColumnToRow(row, atIndex);
        }

        // Update headers and footer
        AddHeaderColumn(table, atIndex);
        AddFooterColumn(table, atIndex);

        table.Columns++;
    }

    public void RemoveColumn(DynamicTable table, int columnIndex)
    {
        if (table.Columns <= 1) return; // Keep at least one column

        if (columnIndex >= 0 && columnIndex < table.Columns)
        {
            foreach (var row in table.TableRows)
            {
                RemoveColumnFromRow(row, columnIndex);
            }

            // Update headers and footer
            RemoveHeaderColumn(table, columnIndex);
            RemoveFooterColumn(table, columnIndex);

            table.Columns--;
        }
    }

    private void AddColumnToRow(TableRow row, int atIndex)
    {
        var newCell = new TableCell
        {
            RowIndex = row.RowIndex,
            ColumnIndex = atIndex,
            ColSpan = 1,
            RowSpan = 1,
            CellType = CellType.StaticText
        };

        row.Cells.Insert(atIndex, newCell);

        // Update column indices
        for (int j = atIndex + 1; j < row.Cells.Count; j++)
        {
            row.Cells[j].ColumnIndex = j;
        }
    }

    private void RemoveColumnFromRow(TableRow row, int columnIndex)
    {
        if (columnIndex < row.Cells.Count)
        {
            row.Cells.RemoveAt(columnIndex);

            // Update column indices
            for (int j = columnIndex; j < row.Cells.Count; j++)
            {
                row.Cells[j].ColumnIndex = j;
            }
        }
    }

    private void ResizeHeaders(DynamicTable table, int newColumns)
    {
        // Ensure Headers list exists
        table.Headers ??= new List<TableHeader>();

        // Add missing headers
        while (table.Headers.Count < newColumns)
        {
            table.Headers.Add(new TableHeader
            {
                Text = $"Column {table.Headers.Count + 1}",
                ColSpan = 1
            });
        }

        // Remove extra headers
        while (table.Headers.Count > newColumns)
        {
            table.Headers.RemoveAt(table.Headers.Count - 1);
        }
    }

    private void ResizeFooter(DynamicTable table, int newColumns)
    {
        // Ensure Footer and Cells exist
        table.Footer ??= new TableFooter();
        table.Footer.Cells ??= new List<TableCell>();

        // Add missing footer cells
        while (table.Footer.Cells.Count < newColumns)
        {
            table.Footer.Cells.Add(new TableCell
            {
                ColumnIndex = table.Footer.Cells.Count,
                ColSpan = 1,
                Text = ""
            });
        }

        // Remove extra footer cells
        while (table.Footer.Cells.Count > newColumns)
        {
            table.Footer.Cells.RemoveAt(table.Footer.Cells.Count - 1);
        }
    }

    private void AddHeaderColumn(DynamicTable table, int atIndex)
    {
        table.Headers ??= new List<TableHeader>();
        if (atIndex < 0) atIndex = table.Headers.Count;
        atIndex = Math.Min(atIndex, table.Headers.Count);

        table.Headers.Insert(atIndex, new TableHeader
        {
            Text = $"Column {atIndex + 1}",
            ColSpan = 1
        });

        for (int j = atIndex + 1; j < table.Headers.Count; j++)
        {
            // Update header indices if needed
        }
    }

    private void RemoveHeaderColumn(DynamicTable table, int columnIndex)
    {
        if (table.Headers != null && columnIndex < table.Headers.Count)
        {
            table.Headers.RemoveAt(columnIndex);
        }
    }

    private void AddFooterColumn(DynamicTable table, int atIndex)
    {
        table.Footer ??= new TableFooter { Cells = new List<TableCell>() };
        table.Footer.Cells ??= new List<TableCell>();
        if (atIndex < 0) atIndex = table.Footer.Cells.Count;

        atIndex = Math.Min(atIndex, table.Footer.Cells.Count);
        table.Footer.Cells.Insert(atIndex, new TableCell
        {
            ColumnIndex = atIndex,
            ColSpan = 1
        });

        for (int j = atIndex + 1; j < table.Footer.Cells.Count; j++)
        {
            table.Footer.Cells[j].ColumnIndex = j;
        }
    }

    private void RemoveFooterColumn(DynamicTable table, int columnIndex)
    {
        if (table.Footer != null && columnIndex < table.Footer.Cells.Count)
        {
            table.Footer.Cells.RemoveAt(columnIndex);

            for (int j = columnIndex; j < table.Footer.Cells.Count; j++)
            {
                table.Footer.Cells[j].ColumnIndex = j;
            }
        }
    }

    public void MapFieldToCell(DynamicTable table, string fieldId, int row, int column,
                             int colSpan = 1, int rowSpan = 1, string label = null)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.FieldId = fieldId;
            cell.ColSpan = Math.Max(1, colSpan);
            cell.RowSpan = Math.Max(1, rowSpan);
            cell.CellType = CellType.Field;
            cell.Label = label;

            ClearOverlappingCells(table, row, column, colSpan, rowSpan, cell);
        }
    }

    public void SetCellText(DynamicTable table, int row, int column, string text, int colSpan = 1, int rowSpan = 1)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.Text = text;
            cell.ColSpan = Math.Max(1, colSpan);
            cell.RowSpan = Math.Max(1, rowSpan);
            cell.CellType = CellType.StaticText;
            cell.FieldId = null;
            cell.Expression = null;

            ClearOverlappingCells(table, row, column, colSpan, rowSpan, cell);
        }
    }

    public void SetCellCalculation(DynamicTable table, int row, int column, string expression, int colSpan = 1, int rowSpan = 1)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.Expression = expression;
            cell.ColSpan = Math.Max(1, colSpan);
            cell.RowSpan = Math.Max(1, rowSpan);
            cell.CellType = CellType.Calculation;
            cell.FieldId = null;
            cell.Text = null;

            ClearOverlappingCells(table, row, column, colSpan, rowSpan, cell);
        }
    }

    public void AddNestedTable(DynamicTable table, int row, int column, DynamicTable nestedTable)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            nestedTable.NestingLevel = table.NestingLevel + 1;
            nestedTable.ParentCellId = cell.Id;
            cell.NestedTable = nestedTable;
            cell.CellType = CellType.NestedTable;
            cell.ColSpan = Math.Max(1, Math.Max(cell.ColSpan, nestedTable.Columns));
            cell.RowSpan = Math.Max(1, Math.Max(cell.RowSpan, nestedTable.Rows));
        }
    }

    public void RemoveNestedTable(DynamicTable table, int row, int column)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.NestedTable = null;
            cell.CellType = CellType.StaticText;
            cell.ColSpan = 1;
            cell.RowSpan = 1;
        }
    }

    public void ClearCell(DynamicTable table, int row, int column)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.FieldId = null;
            cell.Text = null;
            cell.Expression = null;
            cell.NestedTable = null;
            cell.CellType = CellType.StaticText;
            cell.ColSpan = 1;
            cell.RowSpan = 1;
            cell.Label = null;
        }
    }

    private void ClearOverlappingCells(DynamicTable table, int startRow, int startColumn,
                                     int colSpan, int rowSpan, TableCell mainCell)
    {
        for (int r = startRow; r < startRow + rowSpan; r++)
        {
            for (int c = startColumn; c < startColumn + colSpan; c++)
            {
                if (r == startRow && c == startColumn) continue;

                var overlappingCell = GetCell(table, r, c);
                if (overlappingCell != null)
                {
                    overlappingCell.FieldId = null;
                    overlappingCell.Text = null;
                    overlappingCell.Expression = null;
                    overlappingCell.NestedTable = null;
                    overlappingCell.ColSpan = 1;
                    overlappingCell.RowSpan = 1;
                    overlappingCell.CellType = CellType.StaticText;
                }
            }
        }
    }

    public void SetCellSpan(DynamicTable table, int row, int column, int colSpan, int rowSpan)
    {
        var cell = GetCell(table, row, column);
        if (cell != null)
        {
            cell.ColSpan = Math.Max(1, colSpan);
            cell.RowSpan = Math.Max(1, rowSpan);
            ClearOverlappingCells(table, row, column, colSpan, rowSpan, cell);
        }
    }

    public void MergeCells(DynamicTable table, int startRow, int startColumn, int endRow, int endColumn)
    {
        var colSpan = endColumn - startColumn + 1;
        var rowSpan = endRow - startRow + 1;
        SetCellSpan(table, startRow, startColumn, colSpan, rowSpan);
    }

    public void SplitCell(DynamicTable table, int row, int column)
    {
        SetCellSpan(table, row, column, 1, 1);
    }

    public void SetHeader(DynamicTable table, int column, string text, int colSpan = 1)
    {
        if (column >= 0 && table.Headers != null && column < table.Headers.Count)
        {
            table.Headers[column].Text = text;
            table.Headers[column].ColSpan = Math.Max(1, colSpan);
        }
    }

    public void SetFooter(DynamicTable table, int column, string text, int colSpan = 1)
    {
        if (column >= 0 && table.Footer != null && column < table.Footer.Cells.Count)
        {
            table.Footer.Cells[column].Text = text;
            table.Footer.Cells[column].ColSpan = Math.Max(1, colSpan);
        }
    }

    public TableCell GetCell(DynamicTable table, int row, int column)
    {
        if (row >= 0 && table.TableRows != null 
            && row < table.TableRows.Count 
            && column >= 0 
            && column < table.TableRows[row].Cells.Count)
        {
            return table.TableRows[row].Cells[column];
        }
        return null;
    }

    public FormField GetFieldInCell(DynamicTable table, int row, int column, FormDefinition formDefinition)
    {
        var cell = GetCell(table, row, column);
        if (cell != null && !string.IsNullOrEmpty(cell.FieldId))
        {
            return formDefinition.Fields.FirstOrDefault(f => f.Id == cell.FieldId);
        }
        return null;
    }

    public object GetCellValue(DynamicTable table, int row, int column, Dictionary<string, object> formValues, FormDefinition formDefinition)
    {
        var cell = GetCell(table, row, column);
        if (cell == null) return null;

        switch (cell.CellType)
        {
            case CellType.Field:
                if (!string.IsNullOrEmpty(cell.FieldId))
                {
                    var field = formDefinition.Fields.FirstOrDefault(f => f.Id == cell.FieldId);
                    if (field != null && formValues.TryGetValue(field.Name, out var value))
                    {
                        return value;
                    }
                }
                break;

            case CellType.Calculation:
                if (!string.IsNullOrEmpty(cell.Expression))
                {
                    return EvaluateCellCalculation(cell.Expression, formValues, new List<DynamicTable> { table }, formDefinition);
                }
                break;

            case CellType.StaticText:
                return cell.Text;

            case CellType.NestedTable:
                return cell.NestedTable;
        }

        return null;
    }

    public List<TableReference> ParseCellReferences(string expression)
    {
        var references = new List<TableReference>();
        if (string.IsNullOrEmpty(expression)) return references;

        // Pattern: TableId[row,col] or [row,col] for current table
        var pattern = @"(\w+)?\[(\d+),(\d+)\]";
        var matches = System.Text.RegularExpressions.Regex.Matches(expression, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var tableId = match.Groups[1].Success ? match.Groups[1].Value : null;
            var row = int.Parse(match.Groups[2].Value);
            var column = int.Parse(match.Groups[3].Value);

            references.Add(new TableReference
            {
                TableId = tableId,
                Row = row,
                Column = column
            });
        }

        return references;
    }

    public string EvaluateCellCalculation(string expression, Dictionary<string, object> allValues, List<DynamicTable> allTables, FormDefinition formDefinition)
    {
        if (string.IsNullOrEmpty(expression)) return null;

        try
        {
            // Replace cell references with actual values
            var evaluatedExpression = expression;
            var references = ParseCellReferences(expression);

            foreach (var reference in references)
            {
                DynamicTable targetTable = allTables.FirstOrDefault(t => t.Id == reference.TableId) ?? allTables.First();
                var cellValue = GetCellValue(targetTable, reference.Row, reference.Column, allValues, formDefinition);
                var stringValue = cellValue?.ToString() ?? "0";

                evaluatedExpression = evaluatedExpression.Replace(
                    $"{reference.TableId}[{reference.Row},{reference.Column}]",
                    stringValue);
            }

            // Use existing calculation service
            return _calculationService.EvaluateCalculation(evaluatedExpression, allValues, formDefinition)?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating cell calculation: {ex.Message}");
            return "#ERROR";
        }
    }

    public void RecalculateTable(DynamicTable table, Dictionary<string, object> formValues, FormDefinition formDefinition, List<DynamicTable> allTables)
    {
        foreach (var row in table.TableRows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.CellType == CellType.Calculation && !string.IsNullOrEmpty(cell.Expression))
                {
                    var result = EvaluateCellCalculation(cell.Expression, formValues, allTables, formDefinition);
                    // Store result in a temporary cache or update form values
                    // This would need to be integrated with your form value system
                }
            }
        }
    }
}
