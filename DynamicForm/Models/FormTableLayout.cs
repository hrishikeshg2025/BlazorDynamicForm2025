namespace DynamicForm.Models;
// Add to DynamicForm.Models namespace

public class FormLayout
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public LayoutType Type { get; set; } = LayoutType.Vertical;
    public List<LayoutSection> Sections { get; set; } = new();
    public List<DynamicTable> Tables { get; set; } = new();
}

public enum LayoutType
{
    Vertical,
    Grid,
    Mixed
}

public class LayoutSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public List<string> FieldIds { get; set; } = new();
    public int Columns { get; set; } = 1;
}

public class FormTable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool ShowHeaders { get; set; } = true;
    public bool ShowFooter { get; set; } = false;
    public List<TableHeader> Headers { get; set; } = new();
    public List<TableRow> TableRows { get; set; } = new();
    public TableFooter Footer { get; set; }
}

public class TableHeader
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; }
    public int ColSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;
    public string CssClass { get; set; }
}

public class TableRow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int RowIndex { get; set; }
    public List<TableCell> Cells { get; set; } = new();
}

//public class TableCell
//{
//    public string Id { get; set; } = Guid.NewGuid().ToString();
//    public int RowIndex { get; set; }
//    public int ColumnIndex { get; set; }
//    public int ColSpan { get; set; } = 1;
//    public int RowSpan { get; set; } = 1;
//    public string FieldId { get; set; } // Reference to FormField
//    public string Label { get; set; }
//    public string CssClass { get; set; }
//    public bool IsHeader { get; set; }
//    public string Text { get; set; } // For static text cells
//}

public class TableFooter
{
    public List<TableCell> Cells { get; set; } = new();
}

// Update FormDefinition to include layout
public partial class FormDefinition
{
    public FormLayout Layout { get; set; } = new();
}

public class DynamicTable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Rows { get; set; } = 1; // Start with 1 row
    public int Columns { get; set; } = 1; // Start with 1 column
    public bool ShowHeaders { get; set; } = true;
    public bool ShowFooter { get; set; } = false;
    public List<TableHeader> Headers { get; set; } = new();
    public List<TableRow> TableRows { get; set; } = new();
    public TableFooter Footer { get; set; } = new();
    public string CssClass { get; set; }
    public int NestingLevel { get; set; } = 0;
    public string ParentCellId { get; set; } // For nested tables
}

public class TableCell
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public int ColSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;
    public string FieldId { get; set; }
    public string Label { get; set; }
    public string Text { get; set; } = ""; // Initialize with empty string
    public string Expression { get; set; }
    public string CssClass { get; set; }
    public bool IsHeader { get; set; }
    public bool IsReadonly { get; set; }
    public DynamicTable NestedTable { get; set; }
    public CellType CellType { get; set; } = CellType.StaticText;

    // Constructor to ensure proper initialization
    public TableCell()
    {
        Text = "";
    }
}

public enum CellType
{
    Field,
    FieldLabel,     // Shows ONLY the field label (no input)
    StaticText,
    Calculation,
    NestedTable
}

public class TableReference
{
    public string TableId { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }

    public override string ToString() => $"{TableId}[{Row},{Column}]";
}

public class CrossTableCalculation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TargetCellId { get; set; }
    public string Expression { get; set; } // e.g., "Table1[1,1] + Table2[2,2]"
    public List<TableReference> Dependencies { get; set; } = new();
}
public class FieldValueChangedEventArgs
{
    public FormField Field { get; set; }
    public object Value { get; set; }
}