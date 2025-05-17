namespace ASE.API.Features.MasterTemplates.Models;

public class MasterTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }  // Active for this year
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    
    // Navigation properties
    public ICollection<MasterTemplateSheet> Sheets { get; set; } = new List<MasterTemplateSheet>();
} 