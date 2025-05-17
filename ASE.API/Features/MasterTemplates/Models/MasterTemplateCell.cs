namespace ASE.API.Features.MasterTemplates.Models;

public class MasterTemplateCell
{
    public int Id { get; set; }
    public string CellAddress { get; set; } = string.Empty; // Excel-like address (e.g., "A1", "B2")
    public string GlobalAddress { get; set; } = string.Empty; // Excel format: "SheetName!CellAddress" (e.g., "Income Statement!B1")
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty; // e.g., "number", "text", "date", etc.
    
    // Foreign keys
    public int MasterTemplateTableId { get; set; }
    
    // Navigation properties
    public MasterTemplateTable MasterTemplateTable { get; set; } = null!;
}
