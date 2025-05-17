namespace ASE.API.Features.MasterTemplates.Models;

public class MasterTemplateSheet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    
    // Foreign keys
    public int MasterTemplateId { get; set; }
    
    // Navigation properties
    public MasterTemplate MasterTemplate { get; set; } = null!;
    public ICollection<MasterTemplateTable> Tables { get; set; } = new List<MasterTemplateTable>();
} 