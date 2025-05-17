namespace ASE.API.Features.FinanceSubmissions.Models;

public class FinanceSubmissionCell
{
    public int Id { get; set; }
    public string CellAddress { get; set; } = string.Empty; // Excel-like address (e.g., "A1", "B2")
    public string GlobalAddress { get; set; } = string.Empty; // Excel format: "SheetName!CellAddress" (e.g., "Income Statement!B1")
    public decimal Value { get; set; } // Numeric value
    public string AggregationType { get; set; } = "monthly"; // monthly, fytd, r12
    
    // Foreign keys
    public int FinanceSubmissionId { get; set; }
    
    // Navigation properties
    public FinanceSubmission FinanceSubmission { get; set; }
}
