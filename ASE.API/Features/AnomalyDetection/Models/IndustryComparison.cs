namespace ASE.API.Features.AnomalyDetection.Models;

public class IndustryComparison
{
    public int Id { get; set; }
    public decimal Benchmark { get; set; }
    public decimal Deviation { get; set; }
    
    // Foreign key
    public int DataPatternId { get; set; }
    
    // Navigation property
    public DataPattern DataPattern { get; set; } = null!;
} 