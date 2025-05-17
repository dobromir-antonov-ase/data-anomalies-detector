namespace ASE.API.Features.MasterTemplates.Models;

public class MasterTemplateTable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    
    // Foreign keys
    public int MasterTemplateSheetId { get; set; }
    
    // Navigation properties
    public MasterTemplateSheet MasterTemplateSheet { get; set; } = null!;
    public ICollection<MasterTemplateCell> Cells { get; set; } = new List<MasterTemplateCell>();
} 