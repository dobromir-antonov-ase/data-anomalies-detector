using ASE.API.Features.FinanceSubmissions.Models;

namespace ASE.API.Features.Dealers.Models;

public class Dealer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<FinanceSubmission> Submissions { get; set; } = new List<FinanceSubmission>();
} 