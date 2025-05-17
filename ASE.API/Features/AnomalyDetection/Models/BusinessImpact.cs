namespace ASE.API.Features.AnomalyDetection.Models;

public class BusinessImpact
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? EstimatedValue { get; set; }
    
    // Foreign keys
    public int? DataAnomalyId { get; set; }
    public int? DataPatternId { get; set; }
    
    // Navigation properties
    public DataAnomaly? DataAnomaly { get; set; }
    public DataPattern? DataPattern { get; set; }
} 