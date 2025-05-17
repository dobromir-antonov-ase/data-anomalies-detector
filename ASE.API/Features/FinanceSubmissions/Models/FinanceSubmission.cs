using ASE.API.Features.Dealers.Models;
using ASE.API.Features.MasterTemplates.Models;

namespace ASE.API.Features.FinanceSubmissions.Models;

public class FinanceSubmission
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Submitted, Approved, Rejected
    public int Month { get; set; }
    public int Year { get; set; }
    
    // Foreign keys
    public int DealerId { get; set; }
    public int MasterTemplateId { get; set; }
    
    // Navigation properties
    public Dealer Dealer { get; set; } = null!;
    public MasterTemplate MasterTemplate { get; set; } = null!;
    public ICollection<FinanceSubmissionCell> Cells { get; set; } = new List<FinanceSubmissionCell>();
} 